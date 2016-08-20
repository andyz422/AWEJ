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
    var hit_wall_lr = ""
    var hit_wall_ud = ""
    
    var text1_1 = SKSpriteNode()
    var text1_2 = SKSpriteNode()
    var text1_3 = SKSpriteNode()
    var text_array = []
    var store_door = SKSpriteNode()
    var battle_door = SKSpriteNode()
    var leave = false
    var destination = ""
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
        
        store_door = self.childNodeWithName("store_door") as! SKSpriteNode
        battle_door = self.childNodeWithName("battle_door") as! SKSpriteNode
        
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
        store_door.zPosition = 10
        battle_door.zPosition = 10
        
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
        
        store_door.physicsBody = SKPhysicsBody(rectangleOfSize: store_door.size)
        store_door.physicsBody!.dynamic = false
        store_door.physicsBody?.categoryBitMask = object_category
        store_door.physicsBody?.collisionBitMask = 1
        store_door.physicsBody?.usesPreciseCollisionDetection = true
        
        battle_door.physicsBody = SKPhysicsBody(rectangleOfSize: battle_door.size)
        battle_door.physicsBody!.dynamic = false
        battle_door.physicsBody?.categoryBitMask = object_category
        battle_door.physicsBody?.collisionBitMask = 1
        battle_door.physicsBody?.usesPreciseCollisionDetection = true

    }
    
    func didBeginContact(contact: SKPhysicsContact) {
        let objects = ["wall_left", "wall_right", "wall_top", "wall_bottom", "store_door", "battle_door"]
        let A = contact.bodyA.node!.name!
        let B = contact.bodyB.node!.name!
        
        leave = (A == "diglett_town" && ["store_door", "battle_door"].contains(B)) || (["store_door", "battle_door"].contains(A) == "door" && B == "diglett_store")
        
        if A == "diglett_town" {
            destination = B
        } else {
            destination = A
        }
        
        if (A == "diglett_town") && (objects.contains(B)) {
            /*if (B == "wall_top" || B == "wall_bottom") {
                hit_wall_ud = B
            } else {
                if B == "store_door" {
                    hit_wall_lr = "wall_left"
                } else if B == "battle_door" {
                    hit_wall_lr = "wall_right"
                } else {
                    hit_wall_lr = B
                }
            }*/
            if contact.contactNormal.dy < -0.9 {
                hit_wall_ud = "wall_bottom"
            } else if contact.contactNormal.dy > 0.9 {
                hit_wall_ud = "wall_top"
            }
            
            if contact.contactNormal.dx < -0.9 {
                hit_wall_lr = "wall_left"
            } else if contact.contactNormal.dx > 0.9 {
                hit_wall_lr = "wall_right"
            }
        }
        
        if (B == "diglett_town") && (objects.contains(A)) {
            /*if (A == "wall_top" || A == "wall_bottom") {
                hit_wall_ud = A
            } else {
                if A == "store_door" {
                    hit_wall_lr = "wall_left"
                } else if A == "battle_door" {
                    hit_wall_lr = "wall_right"
                } else {
                    hit_wall_lr = A
                }
            }*/
            if contact.contactNormal.dy < -0.9 {
                hit_wall_ud = "wall_bottom"
            } else if contact.contactNormal.dy > 0.9 {
                hit_wall_ud = "wall_top"
            }
            
            if contact.contactNormal.dx < -0.9 {
                hit_wall_lr = "wall_left"
            } else if contact.contactNormal.dx > 0.9 {
                hit_wall_lr = "wall_right"
            }
        }
        diglett_inaction = false
    }
    
    func didEndContact(contact: SKPhysicsContact) {
        
        let objects = ["wall_left", "wall_right", "wall_top", "wall_bottom", "store_door", "battle_door"]
        let A = contact.bodyA.node!.name!
        let B = contact.bodyB.node!.name!
        
        if (A == "diglett_town") && (objects.contains(B)) {
            if (B == "wall_top" || B == "wall_bottom") {
                self.hit_wall_ud = ""
            } else {
                self.hit_wall_lr = ""
            }
        }
        
        if (B == "diglett_town") && (objects.contains(A)) {
            if (A == "wall_top" || A == "wall_bottom") {
                self.hit_wall_ud = ""
            } else {
                self.hit_wall_lr = ""
            }
        }
        leave = false
    }
    
    override func update(currentTime: NSTimeInterval) {
        
        if diglett_inaction {
            move(background)
            move(charmander)
            move(wall_left)
            move(wall_right)
            move(wall_top)
            move(wall_bottom)
            move(store_door)
            move(battle_door)
        }
    }
    
    func move(sprite: SKSpriteNode) {
        
        let x = sprite.position.x
        let y = sprite.position.y
        
        if hit_wall_lr == "wall_left" && xDist > 0 {
            if hit_wall_ud == "wall_top" && yDist > 0 {
                sprite.position = CGPointMake(x, y)
            } else if hit_wall_ud == "wall_bottom" && yDist < 0 {
                sprite.position = CGPointMake(x, y)
            } else {
                sprite.position = CGPointMake(x, y - yDist)
            }
        } else if hit_wall_lr == "wall_right" && xDist < 0 {
            if hit_wall_ud == "wall_top" && yDist > 0 {
                sprite.position = CGPointMake(x, y)
            } else if hit_wall_ud == "wall_bottom" && yDist < 0 {
                sprite.position = CGPointMake(x, y)
            } else {
                sprite.position = CGPointMake(x, y - yDist)
            }
        } else {
            if hit_wall_ud == "wall_top" && yDist > 0 {
                sprite.position = CGPointMake(x + xDist, y)
            } else if hit_wall_ud == "wall_bottom" && yDist < 0 {
                sprite.position = CGPointMake(x + xDist, y)
            } else {
                sprite.position = CGPointMake(x + xDist, y - yDist)
            }
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
                
                if leave {
                    let transition = SKTransition.fadeWithColor(UIColor.blackColor(), duration: 1.0)
                    if destination == "store_door" {
                        if let scene = StoreScene(fileNamed: "StoreScene") {
                            let skView = self.view as SKView!
                            skView.ignoresSiblingOrder = true
                            scene.scaleMode = .AspectFill
                            skView.presentScene(scene, transition: transition)
                        }
                    } else if destination == "battle_door"{
                        if let scene = BattleScene(fileNamed: "BattleScene") {
                            let skView = self.view as SKView!
                            skView.ignoresSiblingOrder = true
                            scene.scaleMode = .AspectFill
                            skView.presentScene(scene, transition: transition)
                        }
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
