//
//  GameOverScene.swift
//  game
//
//  Created by Andy Zhu on 8/23/16.
//  Copyright © 2016 DAW. All rights reserved.
//

import UIKit
import SpriteKit

class GameOverScene: SKScene {
    
    override func didMoveToView(view: SKView) {
       

    }
    
    override func touchesBegan(touches: Set<UITouch>, withEvent event: UIEvent?) {
        
        let transition = SKTransition.fadeWithColor(UIColor.blackColor(), duration: 1.0)
        if let scene = TownScene(fileNamed: "TownScene") {
            let skView = self.view as SKView!
            skView.ignoresSiblingOrder = true
            scene.scaleMode = .AspectFill
            skView.presentScene(scene, transition: transition)
        }
    }
}
