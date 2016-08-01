//
//  ViewController.h
//  Maze
//
//  Created by Andy Zhu on 7/31/16.
//  Copyright © 2016 Andy Zhu. All rights reserved.
//

#import <UIKit/UIKit.h>
#import <QuartzCore/CAAnimation.h>

@interface ViewController : UIViewController

@property (strong, nonatomic) IBOutlet UIImageView *pacman;
@property (strong, nonatomic) IBOutlet UIImageView *ghost1;
@property (strong, nonatomic) IBOutlet UIImageView *ghost2;
@property (strong, nonatomic) IBOutlet UIImageView *ghost3;
@property (strong, nonatomic) IBOutlet UIImageView *exit;

@property (strong, nonatomic) IBOutletCollection(UIImageView) NSArray *wall;

@end

