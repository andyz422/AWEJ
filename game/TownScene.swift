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
    var squirtle = SKSpriteNode()
    var background = SKSpriteNode()
    var wall1 = SKSpriteNode()
    var wall2 = SKSpriteNode()
    var wall3 = SKSpriteNode()
    var wall4 = SKSpriteNode()
    var wall5 = SKSpriteNode()
    var wall6 = SKSpriteNode()
    var wall7 = SKSpriteNode()
    var wall8 = SKSpriteNode()
    var hit_wall_lr = ""
    var hit_wall_ud = ""
    var contact1 = SKPhysicsContact()
    var contact2 = SKPhysicsContact()
    
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
    
    let exclamation = SKSpriteNode(imageNamed: "exclamation")
    
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
        squirtle = self.childNodeWithName("squirtle_town") as! SKSpriteNode
        
        wall1 = self.childNodeWithName("wall1") as! SKSpriteNode
        wall2 = self.childNodeWithName("wall2") as! SKSpriteNode
        wall3 = self.childNodeWithName("wall3") as! SKSpriteNode
        wall4 = self.childNodeWithName("wall4") as! SKSpriteNode
        wall5 = self.childNodeWithName("wall5") as! SKSpriteNode
        wall6 = self.childNodeWithName("wall6") as! SKSpriteNode
        wall7 = self.childNodeWithName("wall7") as! SKSpriteNode
        wall8 = self.childNodeWithName("wall8") as! SKSpriteNode
        
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
        wall1.zPosition = 10
        wall2.zPosition = 10
        wall3.zPosition = 10
        wall4.zPosition = 10
        wall5.zPosition = 10
        wall6.zPosition = 10
        wall7.zPosition = 10
        wall8.zPosition = 10
        button.zPosition = 10
        button2.zPosition = 20
        button2p.zPosition = 10
        button3.zPosition = 20
        button3p.zPosition = 10
        diglett.zPosition = 10
        charmander.zPosition = 10
        squirtle.zPosition = 10
        store_door.zPosition = 10
        battle_door.zPosition = 10
        
        button.alpha = 0.75
        button2.alpha = 0.75
        button2p.alpha = 0.75
        button3.alpha = 0.75
        button3p.alpha = 0.75
        
        createPhysicsBody(diglett, shape: "rectangle", dynamic: true, category: diglett_category, collision: 1, contact: object_category, precise: true)
        createPhysicsBody(charmander, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(squirtle, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(wall1, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(wall2, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(wall3, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(wall4, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(wall5, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(wall6, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(wall7, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(wall8, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(store_door, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(battle_door, shape: "rectangle", dynamic: false, category: object_category, collision: 1, contact: 0, precise: true)
        
        can_talk(squirtle)
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
        sprite.physicsBody?.usesPreciseCollisionDetection = precise
    }
    
    
    func didBeginContact(contact: SKPhysicsContact) {
        
        let objects = ["wall1", "wall2", "wall3", "wall4", "wall5", "wall6", "wall7", "wall8", "battle_door", "charmander_town", "squirtle_town"]
        let A = contact.bodyA.node!.name!
        let B = contact.bodyB.node!.name!
        
        leave = (A == "diglett_town" && ["store_door", "battle_door"].contains(B)) || (["store_door", "battle_door"].contains(A) == "door" && B == "diglett_store")
        
        if A == "diglett_town" {
            destination = B
        } else {
            destination = A
        }
        
        if (A == "diglett_town" && objects.contains(B)) || (B == "diglett_town" && objects.contains(A)) {

            if abs(contact.contactNormal.dy) > abs(contact.contactNormal.dx) && contact.contactNormal.dy < 0 {
                hit_wall_ud = "wall_bottom"
            } else if abs(contact.contactNormal.dy) > abs(contact.contactNormal.dx) && contact.contactNormal.dy > 0 {
                hit_wall_ud = "wall_top"
            }
            
            if abs(contact.contactNormal.dx) > abs(contact.contactNormal.dy) && contact.contactNormal.dx < 0 {
                hit_wall_lr = "wall_left"
            } else if abs(contact.contactNormal.dx) > abs(contact.contactNormal.dy) && contact.contactNormal.dx > 0 {
                hit_wall_lr = "wall_right"
            }
        }
        
        diglett_inaction = false
    }
    
    func didEndContact(contact: SKPhysicsContact) {

        let objects = ["wall1", "wall2", "wall3", "wall4", "wall5", "wall6", "wall7", "wall8", "store_door", "battle_door", "charmander_town", "squirtle_town"]
        let A = contact.bodyA.node!.name!
        let B = contact.bodyB.node!.name!
        
        if (A == "diglett_town") && (objects.contains(B)) {
            if hit_wall_ud == "wall_top" || hit_wall_ud == "wall_bottom" {
                hit_wall_ud = ""
            } else {
                hit_wall_lr = ""
            }
        } else if (B == "diglett_town") && (objects.contains(A)) {
            if hit_wall_ud == "wall_top" || hit_wall_ud == "wall_bottom" {
                hit_wall_ud = ""
            } else {
                hit_wall_lr = ""
            }
        }
        leave = false
    }
    
    override func update(currentTime: NSTimeInterval) {
        
        if diglett_inaction {
            move(background)
            move(charmander)
            move(squirtle)
            move(wall1)
            move(wall2)
            move(wall3)
            move(wall4)
            move(wall5)
            move(wall6)
            move(wall7)
            move(wall8)
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
    
    func can_talk(sprite: SKSpriteNode) {
        
        exclamation.position = CGPoint(x: sprite.position.x, y: sprite.position.y + 100)
        exclamation.zPosition = 10
        exclamation.size = CGSize(width: 230, height: 170)
        addChild(exclamation)
        
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
