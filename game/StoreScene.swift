//
//  StoreScene.swift
//  game
//
//  Created by Andy Zhu on 8/8/16.
//  Copyright © 2016 DAW. All rights reserved.
//

import UIKit
import SpriteKit

class StoreScene: SKScene {
    var button = SKSpriteNode(imageNamed:"button")
        
    var button2 = SKSpriteNode(imageNamed:"button2")
    var button2p = SKSpriteNode(imageNamed:"button2p")
    var button2_pressed = false
        
    var button3 = SKSpriteNode(imageNamed:"button3")
    var button3p = SKSpriteNode(imageNamed:"button3p")
    var button3_pressed = false
        
    var diglett = SKSpriteNode(imageNamed:"diglett")
    var charmander = SKSpriteNode(imageNamed:"charmander")
    var background = SKSpriteNode(imageNamed:"background1")
    
    var base = CGRect(x: 0, y: 0, width: 0, height: 0)
        
    var stickActive:Bool = false
    var diglett_inaction = false
    
    override func didMoveToView(view: SKView) {
        button = self.childNodeWithName("button_store") as! SKSpriteNode
        button2 = self.childNodeWithName("button2_store") as! SKSpriteNode
        button2p = self.childNodeWithName("button2p_store") as! SKSpriteNode
        button3 = self.childNodeWithName("button3_store") as! SKSpriteNode
        button3p = self.childNodeWithName("button3p_store") as! SKSpriteNode
        diglett = self.childNodeWithName("diglett_store") as! SKSpriteNode
        charmander = self.childNodeWithName("charmander_store") as! SKSpriteNode
        background = self.childNodeWithName("background_store") as! SKSpriteNode
        
        button.zPosition = 10
        button2.zPosition = 20
        button2p.zPosition = 10
        button3.zPosition = 20
        button3p.zPosition = 10
        diglett.zPosition = 10
        charmander.zPosition = 10
        
        base = button.frame
    }
    
    override func touchesBegan(touches: Set<UITouch>, withEvent event: UIEvent?) {
            
        for touch in touches {
            let location = touch.locationInNode(self)
            stickActive = CGRectContainsPoint(base, location)
                
            if CGRectContainsPoint(button2.frame, location) {
                button2.zPosition = 0
                button2_pressed = true
            }
                
            if CGRectContainsPoint(button3.frame, location) {
                button3.zPosition = 0
                button3_pressed = true
            }
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
        let remove_button = SKAction.removeFromParent()
        
        if (button2_pressed) {
            button2.zPosition = 20
        }
        
        if (button3_pressed) {
            button3.zPosition = 20
        }
        
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
