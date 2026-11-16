import Foundation

extension DossierCharacter {
    // Synthetic demo only: never include a user's character or the reference PDFs.
    public static var demo: DossierCharacter {
        get throws {
            try DossierCharacter(data: Data("""
            <?xml version="1.0" encoding="utf-8"?>
            <character><gameedition>SR4</gameedition><name>New Runner</name><alias>New Runner</alias>
            <metatype>Human</metatype><created>True</created><settings>default.xml</settings>
            <attributes>
            \(["BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL", "EDG", "MAG", "RES"].map {
                "<attribute><name>\($0)</name><value>3</value><totalvalue>3</totalvalue><metatypemin>1</metatypemin><metatypemax>6</metatypemax><metatypeaugmax>9</metatypeaugmax><augmodifier>0</augmodifier></attribute>"
            }.joined())
            </attributes><skills>
            <skill><name>Pistols</name><attribute>AGI</attribute><rating>3</rating><totalvalue>6</totalvalue><default>True</default></skill>
            <skill><name>Perception</name><attribute>INT</attribute><rating>2</rating><totalvalue>5</totalvalue><default>True</default></skill>
            </skills><contacts/><notes/><background/><physicalcmfilled>0</physicalcmfilled><stuncmfilled>0</stuncmfilled></character>
            """.utf8))
        }
    }
}
