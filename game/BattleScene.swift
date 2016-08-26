//
//  BattleScene.swift
//  game
//
//  Created by Andy Zhu on 8/4/16.
//  Copyright © 2016 DAW. All rights reserved.
//

import SpriteKit

class BattleScene: SKScene, SKPhysicsContactDelegate {
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
    var battle_wall_left = SKSpriteNode()
    var battle_wall_right = SKSpriteNode()
    var base = CGRect()
    
    var xDist = CGFloat()
    var yDist = CGFloat()
    var stickActive:Bool = false
    var diglett_inaction = false
    
    var exclamation = SKSpriteNode()
    let text_count = 3
    var text_array: Array<AnyObject>!
    var talk = false
    var talk_count = -1
    
    let player_category = uint_fast32_t(0x1 << 0)
    let enemy_category = uint_fast32_t(0x1 << 1)

    var health_count = 7
    var health_array: Array<AnyObject>!
    
    var enemy_health_count = 7
    var enemy_health_array: Array<AnyObject>!
    
    var leave = false
    
    var timer = NSTimer()

    override func didMoveToView(view: SKView) {
        
        self.physicsWorld.contactDelegate = self
        self.physicsWorld.gravity = CGVectorMake(0, 0)
        
        button = self.childNodeWithName("button_battle") as! SKSpriteNode
        button2 = self.childNodeWithName("button2_battle") as! SKSpriteNode
        button2p = self.childNodeWithName("button2p_battle") as! SKSpriteNode
        button3 = self.childNodeWithName("button3_battle") as! SKSpriteNode
        button3p = self.childNodeWithName("button3p_battle") as! SKSpriteNode
        diglett = self.childNodeWithName("diglett_battle") as! SKSpriteNode
        charmander = self.childNodeWithName("charmander_battle") as! SKSpriteNode
        battle_wall_left = self.childNodeWithName("battle_wall_left") as! SKSpriteNode
        battle_wall_right = self.childNodeWithName("battle_wall_right") as! SKSpriteNode
        
        for i in 1 ... text_count {
            let text = self.childNodeWithName(String(format: "text_battle_%d", i))
            text!.zPosition = 10
            if text_array == nil {
                text_array = [text!]
            } else {
                text_array.append(text!)
            }
        }
        
        for text in text_array {
            text.runAction(SKAction.hide())
        }
    
        for i in 1 ... health_count {
            let health = self.childNodeWithName(String(format: "health%d", i))
            
            if health_array == nil {
                health_array = [health!]
            } else {
                health_array.append(health!)
            }
        }
        
        for i in 1 ... enemy_health_count {
            let enemy_health = self.childNodeWithName(String(format: "health%de", i))
            
            if enemy_health_array == nil {
                enemy_health_array = [enemy_health!]
            } else {
                enemy_health_array.append(enemy_health!)
            }
        }
        
        base = button.frame
        
        button.alpha = 0.75
        button2.alpha = 0.75
        button2p.alpha = 0.75
        button3.alpha = 0.75
        button3p.alpha = 0.75
        
        let charmander_move_right = SKAction.moveByX(800, y: 0, duration: 3.0)
        let charmander_move_left = SKAction.moveByX(-800, y: 0, duration: 3.0)
        let charmander_sequence = SKAction.sequence([charmander_move_right, charmander_move_left])
        let repeat_charmander = SKAction.repeatActionForever(charmander_sequence)
        
        charmander.runAction(repeat_charmander)
        
        createPhysicsBody(diglett, shape: "rectangle", dynamic: true, category: player_category, collision: 1, contact: player_category | enemy_category, precise: true)
        createPhysicsBody(charmander, shape: "rectangle", dynamic: false, category: player_category | enemy_category, collision: 1, contact: player_category | enemy_category, precise: true)
        createPhysicsBody(battle_wall_left, shape: "rectangle", dynamic: false, category: player_category | enemy_category, collision: 1, contact: 0, precise: true)
        createPhysicsBody(battle_wall_right, shape: "rectangle", dynamic: false, category: player_category | enemy_category, collision: 1, contact: 0, precise: true)

        timer = NSTimer(timeInterval: 0.5, target: self, selector: #selector(charmander_shoot), userInfo: nil, repeats: true)
        NSRunLoop.currentRunLoop().addTimer(timer, forMode: NSRunLoopCommonModes)
        
        self.physicsBody = SKPhysicsBody(edgeLoopFromRect: self.frame)
        
    }
    
    func charmander_shoot() {
        let laser = SKSpriteNode(imageNamed:"enemy_laser")
        laser.name = "enemy_laser"
        laser.size = CGSize(width: 10, height: 100)
        shoot_weapon(laser, position: charmander.position, moveBy: CGVector(dx: 0, dy: -self.frame.height - 100 + laser.position.y), duration: 2.0, category: player_category, contact_category: player_category)
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
                if !talk {
                    let bomb = SKSpriteNode(imageNamed:"bomb")
                    bomb.size = CGSize(width: 30, height: 100)
                    bomb.name = "bomb"
                    shoot_weapon(bomb, position: diglett.position, moveBy: CGVector(dx: 0, dy: self.frame.height + 100 - bomb.position.y), duration: 2.0, category: enemy_category, contact_category: enemy_category)
                }
            }
            
            if CGRectContainsPoint(button3.frame, location) {
                button3.zPosition = 0
                button3_pressed = true
                if !talk {
                    let laser = SKSpriteNode(imageNamed:"laser")
                    laser.name = "laser"
                    laser.size = CGSize(width: 10, height: 100)
                    shoot_weapon(laser, position: diglett.position, moveBy: CGVector(dx: 0, dy: self.frame.height + 100 - laser.position.y), duration: 2.0, category: enemy_category, contact_category: enemy_category)
                }
                
                if talk {
                    if !exclamation.hidden {
                        exclamation.runAction(SKAction.hide())
                    }
                    talk_count += 1
                    if talk_count == 0 {
                        text_array[talk_count].runAction(SKAction.unhide())
                    } else {
                        if talk_count < text_array.count {
                            text_array[talk_count - 1].runAction(SKAction.hide())
                        }
                        
                        if talk_count < text_array.count {
                            text_array[talk_count].runAction(SKAction.unhide())
                        } else {
                            text_array[talk_count - 1].runAction(SKAction.hide())
                            talk_count = -1
                            talk = false
                        }
                    }
                }
            }
        }
    }
    
    func shoot_weapon(weapon: SKSpriteNode, position: CGPoint, moveBy: CGVector, duration: Double, category: uint_fast32_t, contact_category: uint_fast32_t) {
        
        createPhysicsBody(weapon, shape: "rectangle", dynamic: true, category: category, collision: 0, contact: contact_category, precise: true)
        
        weapon.position = position
        let shoot_action = SKAction.moveBy(moveBy, duration: duration)
        let shoot_action_done = SKAction.removeFromParent()
        weapon.runAction(SKAction.sequence([shoot_action, shoot_action_done]))
        weapon.zPosition = 5
        addChild(weapon)
    }
    
    func didBeginContact(contact: SKPhysicsContact) {
        
        let transition = SKTransition.fadeWithColor(UIColor.blackColor(), duration: 1.0)

        if contact.bodyA.node!.name != nil && contact.bodyB.node!.name != nil {
            let A = contact.bodyA.node!.name!
            let B = contact.bodyB.node!.name!
        
            if (A == charmander.name! && ["laser", "bomb"].contains(B)) || (B == charmander.name! && ["laser", "bomb"].contains(A)) {
                if enemy_health_count > 2 {
                    enemy_health_array[enemy_health_count - 1].runAction(SKAction.hide())
                    enemy_health_count -= 1
                } else {
                    enemy_health_array[enemy_health_count - 1].runAction(SKAction.hide())
                    timer.invalidate()
                    charmander.removeAllActions()
                    exclamation = SKSpriteNode(imageNamed: "exclamation")
                    exclamation.position = CGPoint(x: charmander.position.x, y: charmander.position.y + (charmander.size.height / 2) + 50)
                    exclamation.zPosition = 10
                    exclamation.size = CGSize(width: 230, height: 170)
                    addChild(exclamation)
                    talk = (A == diglett.name! && B == charmander.name!) || (A == charmander.name! && B == diglett.name!)
                    leave = true
                }
                
            } else if (A == diglett.name! && B == "enemy_laser") || (B == diglett.name! && A == "enemy_laser") {
                if health_count > 2 {
                    health_array[health_count - 1].runAction(SKAction.hide())
                    health_count -= 1
                } else {
                    health_array[health_count - 1].runAction(SKAction.hide())
                    if let scene = GameOverScene(fileNamed: "GameOverScene") {
                        let skView = self.view as SKView!
                        skView.ignoresSiblingOrder = true
                        scene.scaleMode = .AspectFill
                        skView.presentScene(scene, transition: transition)
                    }
                }
            }
            talk = (A == diglett.name! && B == charmander.name!) || (A == charmander.name! && B == diglett.name!)
        }
    }
    
    
    func didEndContact(contact: SKPhysicsContact) {
        talk = false
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
                button.position = CGPointMake(base.midX - (3 *  xDist), base.midY + (3 * yDist))
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