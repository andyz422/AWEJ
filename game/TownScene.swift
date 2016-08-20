//
//  TownScene.swift
//  game
//
//  Created by Andy Zhu on 8/18/16.
//  Copyright © 2016 DAW. All rights reserved.
//

import UIKit
import SpriteKit

class TownScene: SKScene, SKPhysicsContactDelegate {
    
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
    var wall_left = SKSpriteNode()
    var wall_right = SKSpriteNode()
    var wall_top = SKSpriteNode()
    var wall_bottom = SKSpriteNode()
    var hit_wall = String()
    
    var text1_1 = SKSpriteNode()
    var text1_2 = SKSpriteNode()
    var text1_3 = SKSpriteNode()
    var text_array = []
    var door = SKSpriteNode()
    var leave = false
    var base = CGRect()
    
    var xDist = CGFloat()
    var yDist = CGFloat()
    var stickActive:Bool = false
    var diglett_inaction = false
    
    let diglett_category = uint_fast32_t(0x1 << 0)
    let object_category = uint_fast32_t(0x1 << 0)
    let background_category = uint_fast32_t(0x1 << 1)
    
    override func didMoveToView(view: SKView) {
        
        self.physicsWorld.contactDelegate = self
        self.physicsWorld.gravity = CGVectorMake(0, 0)
        
        background = self.childNodeWithName("town") as! SKSpriteNode
        button = self.childNodeWithName("button_town") as! SKSpriteNode
        button2 = self.childNodeWithName("button2_town") as! SKSpriteNode
        button2p = self.childNodeWithName("button2p_town") as! SKSpriteNode
        button3 = self.childNodeWithName("button3_town") as! SKSpriteNode
        button3p = self.childNodeWithName("button3p_town") as! SKSpriteNode
        diglett = self.childNodeWithName("diglett_town") as! SKSpriteNode
        charmander = self.childNodeWithName("charmander_town") as! SKSpriteNode
        
        wall_left = self.childNodeWithName("wall_left") as! SKSpriteNode
        wall_right = self.childNodeWithName("wall_right") as! SKSpriteNode
        wall_top = self.childNodeWithName("wall_top") as! SKSpriteNode
        wall_bottom = self.childNodeWithName("wall_bottom") as! SKSpriteNode
        
        /*text1_1 = self.childNodeWithName("text1_1") as! SKSpriteNode
        text1_2 = self.childNodeWithName("text1_2") as! SKSpriteNode
        text1_3 = self.childNodeWithName("text1_3") as! SKSpriteNode
        text_array = [text1_1, text1_2, text1_3]*/
        //door = self.childNodeWithName("door") as! SKSpriteNode
        
        /*for text in text_array {
            if text as! SKSpriteNode != text1_1 {
                text.runAction(SKAction.hide())
            }
        }*/
        
        base = button.frame
        
        background.zPosition = 0
        wall_left.zPosition = 10
        wall_right.zPosition = 10
        wall_top.zPosition = 10
        wall_bottom.zPosition = 10
        button.zPosition = 10
        button2.zPosition = 20
        button2p.zPosition = 10
        button3.zPosition = 20
        button3p.zPosition = 10
        diglett.zPosition = 10
        charmander.zPosition = 10
        //door.zPosition = 10
        
        button.alpha = 0.75
        button2.alpha = 0.75
        button2p.alpha = 0.75
        button3.alpha = 0.75
        button3p.alpha = 0.75
        
        diglett.physicsBody = SKPhysicsBody(rectangleOfSize: diglett.size)
        diglett.physicsBody!.dynamic = true
        diglett.physicsBody?.categoryBitMask = diglett_category
        diglett.physicsBody?.collisionBitMask = 1
        diglett.physicsBody?.contactTestBitMask = object_category
        diglett.physicsBody?.usesPreciseCollisionDetection = true
        
        charmander.physicsBody = SKPhysicsBody(rectangleOfSize: charmander.size)
        charmander.physicsBody!.dynamic = false
        charmander.physicsBody?.categoryBitMask = object_category
        charmander.physicsBody?.collisionBitMask = 1
        charmander.physicsBody?.usesPreciseCollisionDetection = true
        
        wall_left.physicsBody = SKPhysicsBody(rectangleOfSize: wall_left.size)
        wall_left.physicsBody!.dynamic = false
        wall_left.physicsBody?.categoryBitMask = object_category
        wall_left.physicsBody?.collisionBitMask = 1
        wall_left.physicsBody?.usesPreciseCollisionDetection = true
        
        wall_right.physicsBody = SKPhysicsBody(rectangleOfSize: wall_right.size)
        wall_right.physicsBody!.dynamic = false
        wall_right.physicsBody?.categoryBitMask = object_category
        wall_right.physicsBody?.collisionBitMask = 1
        wall_right.physicsBody?.usesPreciseCollisionDetection = true
        
        wall_top.physicsBody = SKPhysicsBody(rectangleOfSize: wall_top.size)
        wall_top.physicsBody!.dynamic = false
        wall_top.physicsBody?.categoryBitMask = object_category
        wall_top.physicsBody?.collisionBitMask = 1
        wall_top.physicsBody?.usesPreciseCollisionDetection = true
        
        wall_bottom.physicsBody = SKPhysicsBody(rectangleOfSize: wall_bottom.size)
        wall_bottom.physicsBody!.dynamic = false
        wall_bottom.physicsBody?.categoryBitMask = object_category
        wall_bottom.physicsBody?.collisionBitMask = 1
        wall_bottom.physicsBody?.usesPreciseCollisionDetection = true
        
    }
    
    func didBeginContact(contact: SKPhysicsContact) {
        /*leave = (contact.bodyA.node!.name == "diglett_store" && contact.bodyB.node!.name == "door") || (contact.bodyA.node!.name == "door" && contact.bodyB.node!.name == "diglett_store")*/
        if (contact.bodyA.node!.name! == "diglett_town") && (["wall_left", "wall_right", "wall_top", "wall_bottom"].contains(contact.bodyB.node!.name!)) {
            hit_wall = contact.bodyB.node!.name!
        }
        
        if (contact.bodyB.node!.name! == "diglett_town") && (["wall_left", "wall_right", "wall_top", "wall_bottom"].contains(contact.bodyA.node!.name!)) {
            hit_wall = contact.bodyA.node!.name!
        }
        diglett_inaction = false
    }
    
    func didEndContact(contact: SKPhysicsContact) {
        hit_wall = ""
    }
    
    override func update(currentTime: NSTimeInterval) {
        
        if diglett_inaction {
            move(background)
            move(charmander)
            move(wall_left)
            move(wall_right)
            move(wall_top)
            move(wall_bottom)
        }
    }
    
    func move(sprite: SKSpriteNode) {
        if hit_wall == "" {
            sprite.position = CGPointMake(sprite.position.x + xDist, sprite.position.y - yDist)
        } else if hit_wall == "wall_left" {
            
        } else if hit_wall == "wall_right" {
            
        } else if hit_wall == "wall_top" {
            
        } else if hit_wall == "wall_bottom" {
            
        }
        
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
                button3p.zPosition = 0
                button3_pressed = true
                
                /*if leave {
                    let transition = SKTransition.fadeWithColor(UIColor.blackColor(), duration: 1.0)
                    self.view?.presentScene(TownScene(fileNamed: "TownScene")!, transition: transition)
                }*/
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
