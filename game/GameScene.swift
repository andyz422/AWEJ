//
//  GameScene.swift
//  game
//
//  Created by Andy Zhu on 8/4/16.
//  Copyright © 2016 DAW. All rights reserved.
//

import SpriteKit

class GameScene: SKScene {
    
    override func didMoveToView(view: SKView) {
        
        self.backgroundColor = SKColor.blackColor()
        self.anchorPoint = CGPointMake(0.5, 0.5)
        
        let button = SKSpriteNode(imageNamed: "button")
        let background1 = SKSpriteNode(imageNamed:"background1")

        
    }
    
    override func touchesBegan(touches: Set<UITouch>, withEvent event: UIEvent?) {
        for touch in (touches) {
            let location = touch.locationInNode(self)
        }
    }
    
    /*override func touchesMoved(touches: Set<UITouch>, withEvent event: UIEvent?) {
        for touch in (touches as! Set<UITouch>) {
            let location = touch.locationInNode(self)
            
            let v = CGVector(dx: location.x - button.position.x, dy: location.y - button.position.y)
            let angle = atan2(v.dy, v.dx)
            
            let deg = angle * CGFloat(180 / M_PI)
            print(deg + 180)
            
            let length:CGFloat = button.frame.size.height / 2
            
            let xDist:CGFloat = sin(angle) * length
            let yDist:CGFloat = cos(angle) * length
            
            button.position = CGPointMake(button.position.x - xDist, button.position.y - yDist)
        }
    }*/
}
