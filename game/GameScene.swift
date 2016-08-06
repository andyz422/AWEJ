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
    let charmander = SKSpriteNode(imageNamed:"charmander")
    let laser = SKSpriteNode(imageNamed:"laser")
    let base = CGRect(x: -200, y: -840, width: 100, height: 100)
    
    var stickActive:Bool = false
    
    override func didMoveToView(view: SKView) {
        
        self.anchorPoint = CGPointMake(0.5, 0.5)

        self.addChild(button)
        button.position = CGPointMake(-200, -840)
        button.size.width = 100
        button.size.height = 100
        button.zPosition = 10
        
        self.addChild(diglett)
        diglett.position = CGPointMake(0, -600)
        diglett.size.height = 100
        diglett.size.width = 100
        button.zPosition = 10
        
        self.addChild(charmander)
        charmander.position = CGPointMake(0, 800)
        charmander.size.width = 100
        charmander.size.height = 100
        charmander.zPosition = 10
        
        self.addChild(laser)
        laser.position = CGPointMake(0, 400)
        laser.size.width = 50
        laser.size.height = 100
        laser.zPosition = 10
        
    }
    
    override func touchesBegan(touches: Set<UITouch>, withEvent event: UIEvent?) {
        for touch in touches {
            let location = touch.locationInNode(self)
            
            if (CGRectContainsPoint(base, location)) {
                stickActive = true
            } else {
                stickActive = false
            }
        }
    }
    
    override func touchesMoved(touches: Set<UITouch>, withEvent event: UIEvent?) {
        
        for touch in (touches as! Set<UITouch>) {
            let location = touch.locationInNode(self)
            
            if (stickActive) {
                let v = CGVector(dx: location.x - base.origin.x, dy: location.y - base.origin.y)
                let angle = atan2(v.dy, v.dx)
            
                let length:CGFloat = 40
            
                let xDist:CGFloat = sin(angle - 1.57079633) * length
                let yDist:CGFloat = cos(angle - 1.57079633) * length
                
                if (CGRectContainsPoint(base, location)) {
                    button.position = location
                
                } else {
                    button.position = CGPointMake(base.origin.x - xDist, base.origin.y + yDist)
                }
                diglett.zRotation = angle - 1.57079633
            }
        }
    }
    
    override func touchesEnded(touches: Set<UITouch>, withEvent event: UIEvent?) {
        if (stickActive == true) {
            let move:SKAction = SKAction.moveTo(base.origin, duration: 0.2)
            move.timingMode = .EaseOut
            
            button.runAction(move)
        }
    }
}