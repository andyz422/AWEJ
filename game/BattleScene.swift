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

    override func didMoveToView(view: SKView) {
        
        self.physicsWorld.contactDelegate = self
        self.physicsWorld.gravity = CGVectorMake(0, 0)
        
        button = self.childNodeWithName("button") as! SKSpriteNode
        button2 = self.childNodeWithName("button2") as! SKSpriteNode
        button2p = self.childNodeWithName("button2p") as! SKSpriteNode
        button3 = self.childNodeWithName("button3") as! SKSpriteNode
        button3p = self.childNodeWithName("button3p") as! SKSpriteNode
        diglett = self.childNodeWithName("diglett") as! SKSpriteNode
        charmander = self.childNodeWithName("charmander") as! SKSpriteNode
        
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
        
        diglett.physicsBody = SKPhysicsBody(rectangleOfSize: diglett.size)
        diglett.physicsBody!.dynamic = true
        diglett.physicsBody?.categoryBitMask = diglett_category
        diglett.physicsBody?.contactTestBitMask = charmander_category
        diglett.physicsBody?.collisionBitMask = 1
        diglett.physicsBody?.usesPreciseCollisionDetection = true
        
        charmander.physicsBody = SKPhysicsBody(rectangleOfSize: charmander.size)
        charmander.physicsBody!.dynamic = true
        charmander.physicsBody?.categoryBitMask = charmander_category
        charmander.physicsBody?.contactTestBitMask = diglett_category
        charmander.physicsBody?.collisionBitMask = 1
        charmander.physicsBody?.usesPreciseCollisionDetection = true
        
        self.physicsBody = SKPhysicsBody(edgeLoopFromRect: self.frame)
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
                shoot_weapon(bomb)
            }
            
            if CGRectContainsPoint(button3.frame, location) {
                button3.zPosition = 0
                button3_pressed = true
                let laser = SKSpriteNode(imageNamed:"laser")
                shoot_weapon(laser)
            }
        }
    }
    
    func shoot_weapon(weapon: SKSpriteNode) {
        /*weapon.physicsBody = SKPhysicsBody(rectangleOfSize: weapon.size)
        weapon.physicsBody!.dynamic = true
        weapon.physicsBody?.categoryBitMask = diglett_category
        weapon.physicsBody?.contactTestBitMask = charmander_category
        weapon.physicsBody?.collisionBitMask = 1
        weapon.physicsBody?.usesPreciseCollisionDetection = true*/
        
        weapon.position = diglett.position
        weapon.size = CGSize(width: 50, height: 100)
        addChild(weapon)
        let shoot_action = SKAction.moveBy(CGVector(dx: 0, dy: self.frame.height + 200 - weapon.position.y), duration: 3)
        let shoot_action_done = SKAction.removeFromParent()
        weapon.runAction(SKAction.sequence([shoot_action, shoot_action_done]))
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