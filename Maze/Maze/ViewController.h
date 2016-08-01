//
//  ViewController.h
//  Maze
//
//  Created by Andy Zhu on 7/31/16.
//  Copyright © 2016 Andy Zhu. All rights reserved.
//

#import <UIKit/UIKit.h>
#import <QuartzCore/CAAnimation.h>
#import <CoreMotion/CoreMotion.h>
#define kUpdateInterval (1.0f / 60.0f)

@interface ViewController : UIViewController

@property (strong, nonatomic) IBOutlet UIImageView *pacman;
@property (strong, nonatomic) IBOutlet UIImageView *ghost1;
@property (strong, nonatomic) IBOutlet UIImageView *ghost2;
@property (strong, nonatomic) IBOutlet UIImageView *ghost3;
@property (strong, nonatomic) IBOutlet UIImageView *exit;

@property (strong, nonatomic) IBOutletCollection(UIImageView) NSArray *wall;

@property (assign, nonatomic) CGPoint currentPoint;
@property (assign, nonatomic) CGPoint previousPoint;
@property (assign, nonatomic) CGFloat pacmanXVelocity;
@property (assign, nonatomic) CGFloat pacmanYVelocity;
@property (assign, nonatomic) CGFloat angle;
@property (assign, nonatomic) CMAcceleration acceleration;
@property (strong, nonatomic) CMMotionManager *motionManager;
@property (strong, nonatomic) NSOperationQueue *queue;
@property (strong, nonatomic) NSDate *lastUpdateTime;


@end

