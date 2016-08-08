//
//  GameScene.swift
//  game
//
//  Created by Andy Zhu on 8/4/16.
//  Copyright © 2016 DAW. All rights reserved.
//

import SpriteKit

class GameScene: SKScene {
    var button = SKSpriteNode(imageNamed:"button")
    var diglett = SKSpriteNode(imageNamed:"diglett")
    var charmander = SKSpriteNode(imageNamed:"charmander")
    var laser = SKSpriteNode(imageNamed:"laser")
    var base = CGRect(x: 0, y: 0, width: 0, height: 0)
    
    var stickActive:Bool = false
    
    var diglett_inaction = false

    override func didMoveToView(view: SKView) {
        button = self.childNodeWithName("button") as! SKSpriteNode
        diglett = self.childNodeWithName("diglett") as! SKSpriteNode
        charmander = self.childNodeWithName("charmander") as! SKSpriteNode
        laser = self.childNodeWithName("laser") as! SKSpriteNode
        
        button.zPosition = 10
        diglett.zPosition = 10
        charmander.zPosition = 10
        laser.zPosition = 10
        
        base = button.frame
        
        let charmander_move_right = SKAction.moveByX(800, y: 0, duration: 3.0)
        let charmander_move_left = SKAction.moveByX(-800, y: 0, duration: 3.0)
        let charmander_sequence = SKAction.sequence([charmander_move_right, charmander_move_left])
        let repeat_charmander = SKAction.repeatActionForever(charmander_sequence)
        
        charmander.runAction(repeat_charmander)
    }
    
    override func touchesBegan(touches: Set<UITouch>, withEvent event: UIEvent?) {
        
        for touch in touches {
            let location = touch.locationInNode(self)
            stickActive = CGRectContainsPoint(base, location)
        }
    }
    
    override func touchesMoved(touches: Set<UITouch>, withEvent event: UIEvent?) {
        
        
        for touch in touches {
            let location = touch.locationInNode(self)
            if (diglett_inaction) {
                diglett.removeActionForKey("aKey")
            }
            
            if (stickActive) {
                let v = CGVector(dx: location.x - base.midX, dy: location.y - base.midY)
                let angle = atan2(v.dy, v.dx)
            
                let length:CGFloat = 40
            
                var xDist:CGFloat = sin(angle - 1.57079633) * length
                var yDist:CGFloat = cos(angle - 1.57079633) * length
                
                if (CGRectContainsPoint(base, location)) {
                    xDist = xDist / 2
                    yDist = yDist / 2

                    button.position = location
                
                } else {
                    button.position = CGPointMake(base.midX - xDist, base.midY + yDist)
                }
                
                let action_move = SKAction.moveBy(CGVectorMake(-xDist, yDist), duration: 0.05)
                let repeat_action = SKAction.repeatActionForever(action_move)
                
                diglett.runAction(repeat_action, withKey: "aKey")
                diglett_inaction = true
            }
        }
    }
    
    override func touchesEnded(touches: Set<UITouch>, withEvent event: UIEvent?) {
        
        if (diglett_inaction) {
            diglett.removeActionForKey("aKey")
        }
        if (stickActive == true) {
            let move:SKAction = SKAction.moveTo(CGPoint(x: base.midX, y: base.midY), duration: 0.2)
            move.timingMode = .EaseOut
            
            button.runAction(move)
        }
    }
}