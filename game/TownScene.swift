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
    var house1 = SKSpriteNode()
    var house2 = SKSpriteNode()
    var house3 = SKSpriteNode()
    var house4 = SKSpriteNode()
    var house5 = SKSpriteNode()
    var house6 = SKSpriteNode()
    var house7 = SKSpriteNode()
    var house8 = SKSpriteNode()
    var inn = SKSpriteNode()
    var cow1 = SKSpriteNode()
    var cow2 = SKSpriteNode()
    var cow3 = SKSpriteNode()
    var cow4 = SKSpriteNode()
    var store = SKSpriteNode()
    var mugger = SKSpriteNode()
    var thief = SKSpriteNode()
    
    var hit_wall_lr = ""
    var hit_wall_ud = ""
    var contact1 = SKPhysicsContact()
    var contact1_nil = true
    
    var store_door = SKSpriteNode()
    var battle_door = SKSpriteNode()
    var leave = false
    var destination = ""
 
    
    var base = CGRect()
    
    var xDist = CGFloat()
    var yDist = CGFloat()
    var stickActive:Bool = false
    var diglett_inaction = false
    var pos = CGPoint()
    
    let diglett_category = uint_fast32_t(0x1 << 0)
    let object_category = uint_fast32_t(0x1 << 1)
    let background_category = uint_fast32_t(0x1 << 1)
    
    var exclamations: Array<SKSpriteNode>!
    var objects = ["wall1", "wall2", "wall3", "wall4", "wall5", "wall6", "wall7", "wall8", "store_door", "battle_door", "charmander_town", "squirtle_town", "house1", "house2", "house3", "house4", "house5", "house6", "house7", "house8", "inn", "cow1", "cow2", "cow3", "cow4", "store", "mugger", "thief"]
    
    var text1_1 = SKSpriteNode()
    var text1_2 = SKSpriteNode()
    var text1_3 = SKSpriteNode()
    var text_cow1 = SKSpriteNode()
    var text_cow2 = SKSpriteNode()
    var text_cow3 = SKSpriteNode()
    var text_cow4 = SKSpriteNode()
    var text_array: Array<Array<SKSpriteNode>!>!
    var current_text_array: Array<SKSpriteNode>!
    var talkers: Array<String>!
    var talk = false
    var talkTo = String()
    var talk_count = -1
    
    
    override func didMoveToView(view: SKView) {
        
        self.physicsWorld.contactDelegate = self
        self.physicsWorld.gravity = CGVectorMake(0, 0)

        //background = self.childNodeWithName("town") as! SKSpriteNode
        button = self.childNodeWithName("button_town") as! SKSpriteNode
        button2 = self.childNodeWithName("button2_town") as! SKSpriteNode
        button2p = self.childNodeWithName("button2p_town") as! SKSpriteNode
        button3 = self.childNodeWithName("button3_town") as! SKSpriteNode
        button3p = self.childNodeWithName("button3p_town") as! SKSpriteNode
        diglett = self.childNodeWithName("diglett_town") as! SKSpriteNode
        pos = diglett.position
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
        
        house1 = self.childNodeWithName("house1") as! SKSpriteNode
        house2 = self.childNodeWithName("house2") as! SKSpriteNode
        house3 = self.childNodeWithName("house3") as! SKSpriteNode
        house4 = self.childNodeWithName("house4") as! SKSpriteNode
        house5 = self.childNodeWithName("house5") as! SKSpriteNode
        house6 = self.childNodeWithName("house6") as! SKSpriteNode
        house7 = self.childNodeWithName("house7") as! SKSpriteNode
        house8 = self.childNodeWithName("house8") as! SKSpriteNode
        inn = self.childNodeWithName("inn") as! SKSpriteNode
        cow1 = self.childNodeWithName("cow1") as! SKSpriteNode
        cow2 = self.childNodeWithName("cow2") as! SKSpriteNode
        cow3 = self.childNodeWithName("cow3") as! SKSpriteNode
        cow4 = self.childNodeWithName("cow4") as! SKSpriteNode
        store = self.childNodeWithName("store") as! SKSpriteNode
        mugger = self.childNodeWithName("mugger") as! SKSpriteNode
        thief = self.childNodeWithName("thief") as! SKSpriteNode
        
        store_door = self.childNodeWithName("store_door") as! SKSpriteNode
        battle_door = self.childNodeWithName("battle_door") as! SKSpriteNode
        
        text1_1 = self.childNodeWithName("text_town_1") as! SKSpriteNode
        text1_2 = self.childNodeWithName("text_town_2") as! SKSpriteNode
        text1_3 = self.childNodeWithName("text_town_3") as! SKSpriteNode
        text_cow1 = self.childNodeWithName("text_cow1") as! SKSpriteNode
        text_cow2 = self.childNodeWithName("text_cow2") as! SKSpriteNode
        text_cow3 = self.childNodeWithName("text_cow3") as! SKSpriteNode
        text_cow4 = self.childNodeWithName("text_cow4") as! SKSpriteNode
        
        text_array = [[text1_1, text1_2, text1_3], [text1_1, text1_2], [text_cow1], [text_cow2], [text_cow3], [text_cow4], [text_cow3]]
        
        for texts in text_array {
            for text in texts {
                text.runAction(SKAction.hide())
            }
        }
        
        text1_1.zPosition = 20
        text1_2.zPosition = 20
        text1_3.zPosition = 20
        text_cow1.zPosition = 20
        text_cow2.zPosition = 20
        text_cow3.zPosition = 20
        text_cow4.zPosition = 20
        
        base = button.frame
        //door = self.childNodeWithName("door") as! SKSpriteNode

        //background.zPosition = 0
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
        store_door.zPosition = 11
        battle_door.zPosition = 10
        
        house1.zPosition = 10
        house2.zPosition = 10
        house3.zPosition = 10
        house4.zPosition = 10
        house5.zPosition = 10
        house6.zPosition = 10
        house7.zPosition = 10
        house8.zPosition = 10
        inn.zPosition = 10
        cow1.zPosition = 10
        cow2.zPosition = 10
        cow3.zPosition = 10
        cow4.zPosition = 10
        store.zPosition = 10
        mugger.zPosition = 10
        thief.zPosition = 10
        
        button.alpha = 0.75
        button2.alpha = 0.75
        button2p.alpha = 0.75
        button3.alpha = 0.75
        button3p.alpha = 0.75
        
        createPhysicsBody(diglett, shape: "rectangle", dynamic: true, category: diglett_category, collision: 0, contact: object_category, precise: true)
        createPhysicsBody(charmander, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(squirtle, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(wall1, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(wall2, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(wall3, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(wall4, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(wall5, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(wall6, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(wall7, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(wall8, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(store_door, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(battle_door, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(house1, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(house2, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(house3, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(house4, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(house5, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(house6, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(house7, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(house8, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(inn, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(cow1, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(cow2, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(cow3, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(cow4, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(store, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(mugger, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        createPhysicsBody(thief, shape: "rectangle", dynamic: false, category: object_category, collision: 0, contact: 0, precise: true)
        
        can_talk(charmander)
        can_talk(squirtle)
        can_talk(cow1)
        can_talk(cow2)
        can_talk(cow3)
        can_talk(cow4)
        can_talk(battle_door)
        
    }
    
    func can_talk(sprite: SKSpriteNode) { // exclamation not appearing
        
        let exclamation = SKSpriteNode(imageNamed: "exclamation")
        exclamation.position = CGPoint(x: sprite.position.x, y: sprite.position.y + (sprite.size.height / 2) + 50)
        exclamation.zPosition = 10
        exclamation.size = CGSize(width: 230, height: 170)
        addChild(exclamation)
        
        if talkers == nil {
            talkers = [sprite.name!]
        } else {
            talkers.append(sprite.name!)
        }
        
        if exclamations == nil {
            exclamations = [exclamation]
        } else {
            exclamations.append(exclamation)
        }
    }
    
    
    func createPhysicsBody(sprite: SKSpriteNode, shape: String, dynamic: Bool, category: uint_fast32_t, collision: uint, contact: uint_fast32_t, precise: Bool) {
        
        if shape == "rectangle" {
            sprite.physicsBody = SKPhysicsBody(rectangleOfSize: CGSize(width: sprite.size.width + 6, height: sprite.size.height + 6))
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
        
        let A = contact.bodyA.node!.name!
        let B = contact.bodyB.node!.name!
        
        //leave and destination are variables used for transitioning scenes
        leave = (A == diglett.name! && [store_door.name!, battle_door.name!].contains(B)) || ([store_door.name!, battle_door.name!].contains(A) && B == diglett.name!)
        
        if leave {
            if A == diglett.name! {
                destination = B
            } else {
                destination = A
            }
        }
        
        talk = (A == diglett.name! && talkers.contains(B)) || (talkers.contains(A) && B == diglett.name!)

        if talk {
            if A == diglett.name! {
                talkTo = B
            } else {
                talkTo = A
            }
        }
        
        // detecting collisions, and using hit_wall_ud and hit_wall_lr to signal movement constraints in move()
        // contact1 references the ud contact
        if (A == diglett.name! && objects.contains(B)) || (B == diglett.name! && objects.contains(A)) {

            if abs(contact.contactNormal.dy) > abs(contact.contactNormal.dx) && contact.contactNormal.dy < 0 {
                hit_wall_ud = "wall_bottom"
                contact1 = contact
                contact1_nil = false

            } else if abs(contact.contactNormal.dy) > abs(contact.contactNormal.dx) && contact.contactNormal.dy > 0 {
                hit_wall_ud = "wall_top"
                contact1 = contact
                contact1_nil = false
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
        
        let A = contact.bodyA.node!.name!
        let B = contact.bodyB.node!.name!
        
        if A == diglett.name! && (objects.contains(B)) {
            if !contact1_nil {
                if  (B == contact1.bodyA.node!.name! || B == contact1.bodyB.node!.name) { // Not robust; falsely frees ud constraint and maintains lr constraint when in ud & lr contact with the same node
                    hit_wall_ud = ""
                    contact1 = SKPhysicsContact()
                    contact1_nil = true
                } else {
                    hit_wall_lr = ""
                }
            } else {
                hit_wall_lr = ""
            }
            
        } else if B == diglett.name! && (objects.contains(A)) {
            if !contact1_nil {
                if (A == contact1.bodyA.node!.name! || A == contact1.bodyB.node!.name) { // Not robust; falsely frees ud constraint and maintains lr constraint when in ud & lr contact with the same node
                    hit_wall_ud = ""
                    contact1 = SKPhysicsContact()
                    contact1_nil = true
                } else {
                    hit_wall_lr = ""
                }
            } else {
                hit_wall_lr = ""
            }
            
        }
        leave = false
        talk = false
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
            move(house1)
            move(house2)
            move(house3)
            move(house4)
            move(house5)
            move(house6)
            move(house7)
            move(house8)
            move(inn)
            move(cow1)
            move(cow2)
            move(cow3)
            move(cow4)
            move(store)
            move(mugger)
            move(thief)
            for exclamation in exclamations {
                move(exclamation)
            }
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
                
                if talk {
                    if let index = talkers.indexOf(talkTo) {
                        exclamations[index].runAction(SKAction.hide())
                        
                        current_text_array = text_array[index]
                        talk_count += 1
                        
                        if talk_count == 0 {
                            current_text_array[talk_count].runAction(SKAction.unhide())
                        } else {
                            if talk_count < current_text_array.count {
                                current_text_array[talk_count - 1].runAction(SKAction.hide())
                            }
                            if talk_count < current_text_array.count {
                                current_text_array[talk_count].runAction(SKAction.unhide())
                            } else {
                                current_text_array[talk_count - 1].runAction(SKAction.hide())
                                talk_count = -1
                                if leave {
                                    let transition = SKTransition.fadeWithColor(UIColor.blackColor(), duration: 1.0)
                                    if destination == store_door.name! {
                                        if let scene = StoreScene(fileNamed: "StoreScene") {
                                            let skView = self.view as SKView!
                                            skView.ignoresSiblingOrder = true
                                            scene.scaleMode = .AspectFill
                                            skView.presentScene(scene, transition: transition)
                                        }
                                    } else if destination == battle_door.name! {
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
                } else {
                    if leave {
                        let transition = SKTransition.fadeWithColor(UIColor.blackColor(), duration: 1.0)
                        if destination == store_door.name! {
                            if let scene = StoreScene(fileNamed: "StoreScene") {
                                let skView = self.view as SKView!
                                skView.ignoresSiblingOrder = true
                                scene.scaleMode = .AspectFill
                                skView.presentScene(scene, transition: transition)
                            }
                        } else if destination == battle_door.name! {
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
                diglett_inaction = talk_count == -1
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
