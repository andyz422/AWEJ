//
//  ViewController.swift
//  FoodTracker
//
//  Created by Andy Zhu on 7/13/16.
//  Copyright © 2016 Andy Zhu. All rights reserved.
//

import UIKit

class ViewController: UIViewController, UITextFieldDelegate {
    
    // MARK: Properties
    @IBOutlet weak var nameTextField: UITextField!
    @IBOutlet weak var mealNameLabel: UILabel!

    override func viewDidLoad() {
        super.viewDidLoad()
        
    //Handle the text field's user input through delegate callbacks.
    nameTextField.delegate = self
    }

    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    // MARK: UITextFieldDelegate
    func textFieldShouldReturn(textField: UITextField) -> Bool {
    //Hidethe keyboard
    textField.resignFirstResponder()
    return true
    }
    func textFieldDidEndEditing(textField: UITextField) {
         mealNameLabel.text = textField.text
    }
    
    // MARK: Actions
    @IBAction func setDefaultLabelText(sender: UIButton) {
        mealNameLabel.text = "Default Text"
    }
    
}

