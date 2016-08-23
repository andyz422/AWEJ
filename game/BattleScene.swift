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
    var base = CGRect()
    
    var xDist = CGFloat()
    var yDist = CGFloat()
    var stickActive:Bool = false
    var diglett_inaction = false
    
    let diglett_category = uint_fast32_t(0x1 << 0)
    let charmander_category = uint_fast32_t(0x1 << 1)
    let weapon_category = uint_fast32_t(0x1 << 2)
    
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
        
        createPhysicsBody(diglett, shape: "rectangle", dynamic: true, category: diglett_category, collision: 1, contact: charmander_category, precise: true)
        createPhysicsBody(charmander, shape: "rectangle", dynamic: true, category: charmander_category, collision: 1, contact: diglett_category, precise: true)
        
        timer = NSTimer(timeInterval: 1.0, target: self, selector: #selector(charmander_shoot), userInfo: nil, repeats: true)
        NSRunLoop.currentRunLoop().addTimer(timer, forMode: NSRunLoopCommonModes)
        
        
        self.physicsBody = SKPhysicsBody(edgeLoopFromRect: self.frame)
        
        
    }
    
    func charmander_shoot() {
        let laser = SKSpriteNode(imageNamed:"laser")
        laser.name = "laser"
        laser.size = CGSize(width: 10, height: 100)
        shoot_weapon(laser, position: charmander.position, moveBy: CGVector(dx: 0, dy: -self.frame.height - 100 + laser.position.y), duration: 2.0, contact_category: diglett_category)
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
                let bomb = SKSpriteNode(imageNamed:"bomb")
                bomb.size = CGSize(width: 30, height: 100)
                bomb.name = "bomb"
                shoot_weapon(bomb, position: diglett.position, moveBy: CGVector(dx: 0, dy: self.frame.height + 100 - bomb.position.y), duration: 2.0, contact_category: charmander_category)
            }
            
            if CGRectContainsPoint(button3.frame, location) {
                button3.zPosition = 0
                button3_pressed = true
                let laser = SKSpriteNode(imageNamed:"laser")
                laser.name = "laser"
                laser.size = CGSize(width: 10, height: 100)
                shoot_weapon(laser, position: diglett.position, moveBy: CGVector(dx: 0, dy: self.frame.height + 100 - laser.position.y), duration: 2.0, contact_category: charmander_category)

            }
        }
    }
    
    func shoot_weapon(weapon: SKSpriteNode, position: CGPoint, moveBy: CGVector, duration: Double, contact_category: uint_fast32_t) {
        
        createPhysicsBody(weapon, shape: "rectangle", dynamic: true, category: weapon_category, collision: 0, contact: contact_category, precise: true)
        
        weapon.position = position
        let shoot_action = SKAction.moveBy(moveBy, duration: duration)
        let shoot_action_done = SKAction.removeFromParent()
        weapon.runAction(SKAction.sequence([shoot_action, shoot_action_done]))
        weapon.zPosition = 5
        addChild(weapon)
    }
    
    func didBeginContact(contact: SKPhysicsContact) {
        if contact.bodyA.node!.name != nil && contact.bodyB.node!.name != nil {
            let A = contact.bodyA.node!.name!
            let B = contact.bodyB.node!.name!
        
            if (A == "charmander_battle" && ["laser", "bomb"].contains(B)) {
                print("charmander hit")
            
            } else if (B == "charmander_battle" && ["laser", "bomb"].contains(A)) {
                print("charmander hit")
            }
            
            if (A == "diglett_battle" && B == "laser") {
                print("diglett hit")
            } else if (B == "diglett_battle" && A == "laser") {
                print("diglett hit")
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