//
//  MenuScene.swift
//  game
//
//  Created by Andy Zhu on 8/21/16.
//  Copyright © 2016 DAW. All rights reserved.
//

import UIKit
import SpriteKit

class MenuScene: SKScene {
    
    var newGame = SKSpriteNode()
    var loadGame = SKSpriteNode()
    var controls = SKSpriteNode()
    var options = SKSpriteNode()
    var quit = SKSpriteNode()
    var button_pressed = String()
    
    override func didMoveToView(view: SKView) {
        
        newGame = self.childNodeWithName("newGame") as! SKSpriteNode
        loadGame = self.childNodeWithName("loadGame") as! SKSpriteNode
        controls = self.childNodeWithName("controls") as! SKSpriteNode
        options = self.childNodeWithName("options") as! SKSpriteNode
        quit = self.childNodeWithName("quit") as! SKSpriteNode
        
        newGame.zPosition = 10
        loadGame.zPosition = 10
        controls.zPosition = 10
        options.zPosition = 10
        quit.zPosition = 10
        
    }
    
    override func touchesBegan(touches: Set<UITouch>, withEvent event: UIEvent?) {
        
        for touch in touches {
            let location = touch.locationInNode(self)
            
            if CGRectContainsPoint(newGame.frame, location) {
                newGame.zPosition = 0
                button_pressed = "newGame"
            }
            if CGRectContainsPoint(loadGame.frame, location) {
                loadGame.zPosition = 0
                button_pressed = "loadGame"
            }
            if CGRectContainsPoint(controls.frame, location) {
                controls.zPosition = 0
                button_pressed = "controls"
            }
            if CGRectContainsPoint(options.frame, location) {
                options.zPosition = 0
                button_pressed = "options"
            }
            if CGRectContainsPoint(quit.frame, location) {
                quit.zPosition = 0
                button_pressed = "quit"
            }
        }
    }
    
    override func touchesEnded(touches: Set<UITouch>, withEvent event: UIEvent?) {
        
        let transition = SKTransition.fadeWithColor(UIColor.blackColor(), duration: 1.0)
        let skView = self.view as SKView!
        
        if button_pressed == "newGame" {
            newGame.zPosition = 10
            if let scene = TownScene(fileNamed:"TownScene") {
                skView.ignoresSiblingOrder = true
                scene.scaleMode = .AspectFill
                skView.presentScene(scene, transition: transition)
            }
        }
        
        if button_pressed == "loadGame" {
            loadGame.zPosition = 10
        }
        
        if button_pressed == "controls" {
            controls.zPosition = 10
        }
        
        if button_pressed == "options" {
            options.zPosition = 10
        }
        
        if button_pressed == "quit" {
            quit.zPosition = 10
            exit(0)
            
        }
    }
}
