// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "ChummerDossier",
    platforms: [.iOS(.v17), .macOS(.v13)],
    products: [.library(name: "DossierCore", targets: ["DossierCore"])],
    targets: [
        .target(name: "DossierCore"),
        .testTarget(name: "DossierCoreTests", dependencies: ["DossierCore"])
    ]
)
