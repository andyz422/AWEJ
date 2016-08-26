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
    var store_wall_left = SKSpriteNode()
    var store_wall_right = SKSpriteNode()
    
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
    
    var touch_count = -1
    
    override func didMoveToView(view: SKView) {
        
        self.physicsWorld.contactDelegate = self
        self.physicsWorld.gravity = CGVectorMake(0, 0)
        
        background = self.childNodeWithName("store_background") as! SKSpriteNode
        store_wall_left = self.childNodeWithName("store_wall_left") as! SKSpriteNode
        store_wall_right = self.childNodeWithName("store_wall_right") as! SKSpriteNode
        
        button = self.childNodeWithName("button_store") as! SKSpriteNode
        button2 = self.childNodeWithName("button2_store") as! SKSpriteNode
        button2p = self.childNodeWithName("button2p_store") as! SKSpriteNode
        button3 = self.childNodeWithName("button3_store") as! SKSpriteNode
        button3p = self.childNodeWithName("button3p_store") as! SKSpriteNode
        diglett = self.childNodeWithName("diglett_store") as! SKSpriteNode
        charmander = self.childNodeWithName("charmander_store") as! SKSpriteNode
        text1_1 = self.childNodeWithName("text_store_1") as! SKSpriteNode
        text1_2 = self.childNodeWithName("text_store_2") as! SKSpriteNode
        text1_3 = self.childNodeWithName("text_store_3") as! SKSpriteNode
        text_array = [text1_1, text1_2, text1_3]
        door = self.childNodeWithName("door_store") as! SKSpriteNode

        for text in text_array {
            if text as! SKSpriteNode != text1_1 {
                text.runAction(SKAction.hide())
            }
        }
        
        base = button.frame
        
        text1_1.zPosition = 10
        text1_2.zPosition = 10
        text1_3.zPosition = 10
        background.zPosition = 0
        button.zPosition = 10
        button2.zPosition = 20
        button2p.zPosition = 10
        button3.zPosition = 20
        button3p.zPosition = 10
        diglett.zPosition = 10
        charmander.zPosition = 10
        door.zPosition = 10
        
        button.alpha = 0.75
        button2.alpha = 0.75
        button2p.alpha = 0.75
        button3.alpha = 0.75
        button3p.alpha = 0.75
        
        createPhysicsBody(diglett, shape: "rectangle", dynamic: true, category: diglett_category, collision: 1, contact: object_category, precise: true)
        createPhysicsBody(charmander, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(door, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(store_wall_left, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(store_wall_right, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)

        
        self.physicsBody = SKPhysicsBody(edgeLoopFromRect: self.frame)
    }
    
    func createPhysicsBody(sprite: SKSpriteNode, shape: String, dynamic: Bool, category: uint_fast32_t, collision: uint, contact: uint_fast32_t, precise: Bool) {
        
        if shape == "rectangle" {
            sprite.physicsBody = SKPhysicsBody(rectangleOfSize: sprite.size)
        } else {
            sprite.physicsBody = SKPhysicsBody(circleOfRadius: sprite.size.height / 2)
        }
        sprite.physicsBody!.dynamic = dynamic
        sprite.physicsBody?.categoryBitMask = category
        sprite.physicsBody?.collisionBitMask = collision
        sprite.physicsBody?.contactTestBitMask = contact
        sprite.physicsBody?.allowsRotation = false
        sprite.physicsBody?.usesPreciseCollisionDetection = precise
    }
    
    func didBeginContact(contact: SKPhysicsContact) {
        leave = (contact.bodyA.node!.name == "diglett_store" && contact.bodyB.node!.name == "door_store") || (contact.bodyA.node!.name == "door_store" && contact.bodyB.node!.name == "diglett_store")
    }
    
    func didEndContact(contact: SKPhysicsContact) {
        leave = false
    }
    
    override func update(currentTime: NSTimeInterval) {
        if diglett_inaction {
            diglett.position = CGPointMake(diglett.position.x - xDist, diglett.position.y + yDist)
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
                button3.zPosition = 0
                button3_pressed = true
                
                if (touch_count + 1 < text_array.count && text_array[touch_count + 1].hidden == true) {
                    break
                }
                touch_count += 1
                

                if touch_count < text_array.count {
                    text_array[touch_count].runAction(SKAction.hide())
                }
                if touch_count + 1 < text_array.count {
                    text_array[touch_count + 1].runAction(SKAction.unhide())
                }
                if (touch_count >= text_array.count) && (leave) {
                    let transition = SKTransition.fadeWithColor(UIColor.blackColor(), duration: 1.0)
                    if let scene = TownScene(fileNamed: "TownScene") {
                        let skView = self.view as SKView!
                        skView.ignoresSiblingOrder = true
                        scene.scaleMode = .AspectFill
                        
                        skView.presentScene(scene, transition: transition)
                    }
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
                
                let length:CGFloat = 10
                
                xDist = sin(angle - 1.57079633) * length
                yDist = cos(angle - 1.57079633) * length
                
                if (CGRectContainsPoint(base, location)) {
                    xDist = xDist / 2
                    yDist = yDist / 2
                    
                }
                button.position = CGPointMake(base.midX - (3 * xDist), base.midY + (3 * yDist))
                diglett_inaction = touch_count >= text_array.count - 1
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
