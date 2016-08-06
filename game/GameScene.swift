//
//  GameScene.swift
//  game
//
//  Created by Andy Zhu on 8/4/16.
//  Copyright © 2016 DAW. All rights reserved.
//

import SpriteKit

class GameScene: SKScene {
    
    let background1 = SKSpriteNode(imageNamed:"background1")
    let panel = SKSpriteNode(imageNamed:"panel")
    let button = SKSpriteNode(imageNamed:"button")
    let diglett = SKSpriteNode(imageNamed:"diglett")
    
    var stickActive:Bool = false
    
    override func didMoveToView(view: SKView) {
        
        self.backgroundColor = SKColor.blackColor()
        self.anchorPoint = CGPointMake(0.5, 0.5)
        
        self.addChild(background1)
        background1.position = CGPointMake(-300, 0)
        
        self.addChild(button)
        button.position = CGPointMake(-50, -300)
        button.size.width = 50
        button.size.height = 50
        
        self.addChild(panel)
        panel.position = CGPointMake(-50, -300)
        panel.size.width = 60
        panel.size.height = 60
        
        self.addChild(diglett)
        diglett.position = CGPointMake(100, 200)
        diglett.size.height = 100
        diglett.size.width = 100
        
    }
    
    override func touchesBegan(touches: Set<UITouch>, withEvent event: UIEvent?) {
        
        for touch in touches {
            let location = touch.locationInNode(self)
            
            
            if (CGRectContainsPoint(button.frame, location)) {
                stickActive = true
            } else {
                stickActive = false
            }
        }
    }
    
    override func touchesMoved(touches: Set<UITouch>, withEvent event: UIEvent?) {
        
        for touch in (touches as! Set<UITouch>) {
            let location = touch.locationInNode(self)
            
            if (stickActive == true) {
                let v = CGVector(dx: location.x - button.position.x, dy: location.y - button.position.y)
                let angle = atan2(v.dy, v.dx)
            
                let length:CGFloat = 40
            
                let xDist:CGFloat = sin(angle - 1.57079633) * length
                let yDist:CGFloat = cos(angle - 1.57079633) * length
                
                if (CGRectContainsPoint(button.frame, location)) {
                button.position = location
                
                } else {
                    button.position = CGPointMake(button.position.x - xDist, button.position.y + yDist)
                }
            }
        }
    }
    
    override func touchesEnded(touches: Set<UITouch>, withEvent event: UIEvent?) {
        if (stickActive == true) {
            let move:SKAction = SKAction.moveTo(button.position, duration: 0.2)
            move.timingMode = .EaseOut
            
            button.runAction(move)
        }
    }
}