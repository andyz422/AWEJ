//
//  StoreScene.swift
//  game
//
//  Created by Andy Zhu on 8/8/16.
//  Copyright © 2016 DAW. All rights reserved.
//

import UIKit
import SpriteKit


class StoreScene: SKScene, SKPhysicsContactDelegate {
    
    var button = SKSpriteNode()
    var button2 = SKSpriteNode()
    var button2p = SKSpriteNode()
    var button2_pressed = false
    var button3 = SKSpriteNode()
    var button3p = SKSpriteNode()
    var button3_pressed = false
    var diglett = SKSpriteNode()
    var charmander = SKSpriteNode()
    var background = SKSpriteNode()
    var text1_1 = SKSpriteNode()
    var text1_2 = SKSpriteNode()
    var text1_3 = SKSpriteNode()
    var text_array = []
    var base = CGRect()
    
    var xDist = CGFloat()
    var yDist = CGFloat()
    var stickActive:Bool = false
    var diglett_inaction = false
    
    let diglett_category = uint_fast32_t(0x1 << 0)
    
    override func didMoveToView(view: SKView) {
        
        self.physicsWorld.contactDelegate = self
        self.physicsWorld.gravity = CGVectorMake(0, 0)
        
        button = self.childNodeWithName("button_store") as! SKSpriteNode
        button2 = self.childNodeWithName("button2_store") as! SKSpriteNode
        button2p = self.childNodeWithName("button2p_store") as! SKSpriteNode
        button3 = self.childNodeWithName("button3_store") as! SKSpriteNode
        button3p = self.childNodeWithName("button3p_store") as! SKSpriteNode
        diglett = self.childNodeWithName("diglett_store") as! SKSpriteNode
        charmander = self.childNodeWithName("charmander_store") as! SKSpriteNode
        text1_1 = self.childNodeWithName("text1_1") as! SKSpriteNode
        text1_2 = self.childNodeWithName("text1_2") as! SKSpriteNode
        text_array = [text1_1, text1_2]

        for text in text_array {
            if text as! SKSpriteNode != text1_1 {
                text.runAction(SKAction.hide())
            }
        }
        
        base = button.frame
        
        button.zPosition = 10
        button2.zPosition = 20
        button2p.zPosition = 10
        button3.zPosition = 20
        button3p.zPosition = 10
        diglett.zPosition = 10
        charmander.zPosition = 10
        
        button.alpha = 0.75
        button2.alpha = 0.75
        button2p.alpha = 0.75
        button3.alpha = 0.75
        button3p.alpha = 0.75
        
        diglett.physicsBody = SKPhysicsBody(rectangleOfSize: diglett.size)
        diglett.physicsBody!.dynamic = true
        diglett.physicsBody?.categoryBitMask = diglett_category
        diglett.physicsBody?.collisionBitMask = 1
        diglett.physicsBody?.usesPreciseCollisionDetection = true
        
        self.physicsBody = SKPhysicsBody(edgeLoopFromRect: self.frame)
        
        
    }
    
    
    override func update(currentTime: NSTimeInterval) {
        if diglett_inaction {
            diglett.position = CGPointMake(diglett.position.x - xDist, diglett.position.y + yDist)
        }
    }
    
    
    override func touchesBegan(touches: Set<UITouch>, withEvent event: UIEvent?) {
        
        var touch_count = -1
        
        for touch in touches {
            
            touch_count += 1
            let location = touch.locationInNode(self)
            stickActive = CGRectContainsPoint(base, location)
                
            if CGRectContainsPoint(button2.frame, location) {
                button2.zPosition = 0
                button2_pressed = true
            }
                
            if CGRectContainsPoint(button3.frame, location) {
                button3.zPosition = 0
                button3_pressed = true
                
                if touch_count < text_array.count {
                    text_array[touch_count].runAction(SKAction.hide())
                }
                if touch_count + 1 < text_array.count {
                    text_array[touch_count + 1].runAction(SKAction.unhide())
                }

            }
        }
    }
    
    
    override func touchesMoved(touches: Set<UITouch>, withEvent event: UIEvent?) {

        for touch in touches {
            let location = touch.locationInNode(self)
            
            if (stickActive) {

                let v = CGVector(dx: location.x - base.midX, dy: location.y - base.midY)
                let angle = atan2(v.dy, v.dx)
                
                let length:CGFloat = 40
                
                xDist = sin(angle - 1.57079633) * length
                yDist = cos(angle - 1.57079633) * length
                
                if (CGRectContainsPoint(base, location)) {
                    xDist = xDist / 2
                    yDist = yDist / 2
                    
                    button.position = location
                    
                } else {
                    button.position = CGPointMake(base.midX - xDist, base.midY + yDist)
                }
                diglett_inaction = true
            }
        }
    }
    
    override func touchesEnded(touches: Set<UITouch>, withEvent event: UIEvent?) {
        
        if (button2_pressed) {
            button2.zPosition = 20
        }
        
        if (button3_pressed) {
            button3.zPosition = 20
        }
        
        diglett_inaction = false
        if (stickActive == true) {
            let move:SKAction = SKAction.moveTo(CGPoint(x: base.midX, y: base.midY), duration: 0.2)
            move.timingMode = .EaseOut
            
            button.runAction(move)
            
        }
    }
}
