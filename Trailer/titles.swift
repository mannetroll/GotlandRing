import AppKit
let folder = CommandLine.arguments[1]
let width = 1920, height = 1080
func render(_ name: String, _ draw: () -> Void) {
    let bitmap = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: width, pixelsHigh: height, bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false, colorSpaceName: .deviceRGB, bytesPerRow: width * 4, bitsPerPixel: 32)!
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: bitmap)
    NSColor.clear.setFill(); NSRect(x: 0, y: 0, width: width, height: height).fill()
    draw()
    NSGraphicsContext.restoreGraphicsState()
    try! bitmap.representation(using: .png, properties: [:])!.write(to: URL(fileURLWithPath: folder + "/" + name + ".png"))
}
func box(_ x: CGFloat, _ y: CGFloat, _ w: CGFloat, _ h: CGFloat, _ color: NSColor) {
    color.setFill(); NSRect(x: x, y: CGFloat(height) - y - h, width: w, height: h).fill()
}
func text(_ value: String, _ x: CGFloat, _ y: CGFloat, _ size: CGFloat, _ condensed: Bool = false, _ color: NSColor = .white) {
    let font = NSFont(name: condensed ? "DINCondensed-Bold" : "ArialMT", size: size)!
    let attributes: [NSAttributedString.Key: Any] = [.font: font, .foregroundColor: color, .kern: condensed ? 2.0 : 1.0]
    let string = NSAttributedString(string: value, attributes: attributes)
    string.draw(at: NSPoint(x: x, y: CGFloat(height) - y - string.size().height))
}
let pale = NSColor(calibratedRed: 0.76, green: 0.88, blue: 0.92, alpha: 1)
let accent = NSColor(calibratedRed: 0.91, green: 0.24, blue: 0.19, alpha: 1)
render("matte") {
    box(0, 0, 1920, 64, .black); box(0, 1016, 1920, 64, .black)
    text("MANNETROLL SOLUTIONS AB", 72, 22, 18, false, pale)
    text("GOTLAND RING  /  v0.3.2", 1490, 22, 18, false, pale)
}
render("opening") {
    box(68, 130, 7, 106, accent)
    text("GOTLAND RING", 96, 120, 88, true)
    text("IMPREZA  /  GAMEPLAY TRAILER", 100, 218, 23, false, pale)
}
for (name, caption) in [("cockpit", "IN THE COCKPIT"), ("slide", "AWD  /  SLIDE PRACTICE"), ("pass", "GUTEMÅLRAKAN  /  FLY-BY")] {
    render(name) { text(caption, 72, 1034, 23, true) }
}
render("closing") {
    box(68, 130, 7, 106, accent)
    text("GOTLAND RING", 96, 120, 88, true)
    text("IMPREZA  /  macOS + WINDOWS", 100, 218, 23, false, pale)
    text("github.com/mannetroll/GotlandRing", 72, 970, 24)
    text("Cars: SpatialNeglect & Mateusz Woliński  /  CC BY-NC 4.0", 72, 1035, 20, false, pale)
}
