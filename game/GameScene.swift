//
//  GameScene.swift
//  game
//
//  Created by Andy Zhu on 8/4/16.
//  Copyright © 2016 DAW. All rights reserved.
//

import SpriteKit

class GameScene: SKScene {
    
    let button = SKSpriteNode(imageNamed:"button")
    let diglett = SKSpriteNode(imageNamed:"diglett")
    
    override func didMoveToView(view: SKView) {
        
        self.backgroundColor = SKColor.blackColor()
        self.anchorPoint = CGPointMake(0.5, 0.5)
        
        self.addChild(button)
        button.position = CGPointMake(200, 800)
        
        self.addChild(diglett)
        diglett.position = CGPointMake(100, 200)
        
    }
    
    override func touchesBegan(touches: Set<UITouch>, withEvent event: UIEvent?) {
        
        for touch in touches {
            let location = touch.locationInNode(self)
        }
    }
    
    override func touchesMoved(touches: Set<UITouch>, withEvent event: UIEvent?) {
        for touch in (touches ) {
            let location = touch.locationInNode(self)
            
            let v = CGVector(dx: location.x - button.position.x, dy: location.y - button.position.y)
            let angle = atan2(v.dy, v.dx)
            
            let deg = angle * CGFloat(180 / M_PI)
            print(deg + 180, terminator: "")
            
            let length:CGFloat = button.frame.size.height / 2
            
            let xDist:CGFloat = sin(angle - 1.57079633) * length
            let yDist:CGFloat = cos(angle - 1.57079633) * length
            
            button.position = CGPointMake(button.position.x - xDist, button.position.y + yDist)
        }
    }
}