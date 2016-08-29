//
//  Object.swift
//  game
//
//  Created by Andy Zhu on 8/28/16.
//  Copyright © 2016 DAW. All rights reserved.
//

import UIKit

class Object: NSObject, NSCoding {
    
    var name: String
    var x: Float
    var y: Float
    
    static let DocumentsDirectory = NSFileManager().URLsForDirectory(.DocumentDirectory, inDomains: .UserDomainMask).first!
    static let ArchiveURL = DocumentsDirectory.URLByAppendingPathComponent("objects")
    
    struct PropertyKey {
        static let nameKey = "name"
        static let xKey = "x"
        static let yKey = "y"
    }
    
    init?(name: String, position: CGPoint) {
        
        self.name = name
        self.x = x
        self.y = y
        
        super.init()
        
        if name.isEmpty {
            return nil
        }
    }
    
    func encodeWithCoder(aCoder: NSCoder) {
        aCoder.encodeObject(name, forKey: PropertyKey.nameKey)
        aCoder.encodeObject(x, forKey: PropertyKey.xKey)
        aCoder.encodeObject(y, forKey: PropertyKey.yKey)
    }
    
    required convenience init?(coder aDecoder: NSCoder) {
        let name = aDecoder.decodeOjbectForKey(PropertyKey.nameKey) as! String
        
        
    }
}