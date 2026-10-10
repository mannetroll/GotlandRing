#!/usr/bin/env python3
"""Extract the supplied Subaru GLB into Unity FBX and Standard material inputs.

Requires assimp and ffmpeg. Normal builds use the prepared assets and do not
need these tools or the original GLB. Source attribution stays in source.json.
"""
import argparse
import copy
import json
from pathlib import Path
import struct
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def prepare(source):
    data = source.read_bytes()
    magic, version, length = struct.unpack_from('<4sII', data)
    if magic != b'glTF' or version != 2 or length != len(data):
        raise ValueError('Expected a complete glTF 2 GLB')
    json_length, _ = struct.unpack_from('<II', data, 12)
    gltf = json.loads(data[20:20 + json_length])
    bin_start = 20 + json_length + 8
    binary = data[bin_start:]
    output = ROOT / 'Assets/Models/SubaruImpreza'
    textures = output / 'Textures'
    geometry = output / 'Source'
    work = ROOT / 'Build/SubaruImport'
    for folder in (textures, geometry, work):
        folder.mkdir(parents=True, exist_ok=True)
    (geometry / 'source.json').write_text(json.dumps(gltf['asset'], indent=2) + '\n')

    def texture(index, name, packed=False, metallic=1, roughness=1):
        image = gltf['images'][gltf['textures'][index]['source']]
        view = gltf['bufferViews'][image['bufferView']]
        start = view.get('byteOffset', 0)
        raw = work / ('embedded-' + str(index) + ('.jpg' if image['mimeType'] == 'image/jpeg' else '.png'))
        raw.write_bytes(binary[start:start + view['byteLength']])
        suffix = '.png' if packed or image['mimeType'] == 'image/png' else '.jpg'
        target = textures / (name + suffix)
        filters = "scale='min(2048,iw)':-1"
        if packed:
            filters += f",format=rgba,geq=r='b(X,Y)*{metallic}':g=0:b=0:a='255-g(X,Y)*{roughness}'"
        subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(raw), '-vf', filters,
                        '-frames:v', '1', str(target)], check=True)
        return str(target.relative_to(output))

    materials = []
    for material in gltf['materials']:
        name = material['name']
        pbr = material['pbrMetallicRoughness']
        record = dict(name=name, color=pbr.get('baseColorFactor', [1, 1, 1, 1]),
                      metallic=pbr.get('metallicFactor', 1), roughness=pbr.get('roughnessFactor', 1),
                      alphaMode=material.get('alphaMode', 'OPAQUE'))
        for field, owner, key, suffix in (
            ('albedo', pbr, 'baseColorTexture', 'BaseColor'),
            ('normal', material, 'normalTexture', 'Normal'),
            ('emission', material, 'emissiveTexture', 'Emission'),
        ):
            if key in owner:
                record[field] = texture(owner[key]['index'], name + '_' + suffix)
        if 'metallicRoughnessTexture' in pbr:
            record['metallicSmoothness'] = texture(pbr['metallicRoughnessTexture']['index'],
                                                  name + '_MetallicSmoothness', True,
                                                  record['metallic'], record['roughness'])
        materials.append(record)
    (geometry / 'materials.json').write_text(json.dumps(dict(materials=materials), indent=2) + '\n')

    # Export geometry without embedding the GLB's 100 MB of source textures.
    model = copy.deepcopy(gltf)
    used_views = sorted({a['bufferView'] for a in model['accessors']})
    remap = {old: new for new, old in enumerate(used_views)}
    payload = bytearray()
    views = []
    for index in used_views:
        view = dict(gltf['bufferViews'][index])
        start = view.get('byteOffset', 0)
        payload.extend(b'\0' * (-len(payload) % 4))
        view['byteOffset'] = len(payload)
        payload.extend(binary[start:start + view['byteLength']])
        views.append(view)
    for accessor in model['accessors']:
        accessor['bufferView'] = remap[accessor['bufferView']]
    model['bufferViews'] = views
    model['buffers'] = [dict(uri='geometry.bin', byteLength=len(payload))]
    model['materials'] = [dict(name=m['name']) for m in gltf['materials']]
    for key in ('images', 'textures', 'samplers', 'extensionsUsed', 'extensionsRequired'):
        model.pop(key, None)
    (work / 'geometry.bin').write_bytes(payload)
    (work / 'geometry.gltf').write_text(json.dumps(model))
    subprocess.run(['assimp', 'export', str(work / 'geometry.gltf'),
                    str(geometry / 'SubaruImpreza.fbx'), '-ffbx'], check=True)
    print('Prepared', output)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', nargs='?', type=Path, default=ROOT / 'Impreza/subaru_impreza.glb')
    prepare(parser.parse_args().source)
