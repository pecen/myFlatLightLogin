# myFlatLightLogin
This is a continued project from the YouTube tutorial "How to Design a Flat Light Login Page in C#" with my add-ons. 

I created this project initially in my Tutorials repository. The following is the description from the ReadMe there:

8. C# Wpf UI | How to Design a Flat Light Login Page in C# - Added 230301, from YouTube: https://www.youtube.com/watch?v=6_qRkGo2sOI&t=1s 
	- Folder: FlatLightLoginPage
	- Solution name: FlatLightLogin.sln
	- Framework: .Net 7.0

I went through that tutorial and then copied the finished solution and started to work with it making my modifications which includes among others:
  - Implementing the MVVM pattern and thus removing code-behind
  - Adding a way to exit (exit button instead of a menu in the title bar)
  - Changing a few things in the look and feel, suiting how I would like to have it. 
  - Added a PasswordBox instead of a TextBox for the password placeholder

For a comparison, open this solution in conjunction with the tutorial solution and run the two applications. 

Also, this is an ongoing development so the look will probably change over time depending on how much time is spent on this project. 
	
**Note:** With the change to a PasswordBox I had to do a few modifications to get the same behavior as the TextBox, since a PasswordBox differs a lot from a TextBox. For instance the propterty Text's equivalence in a PasswordBox is the properties Password or SecurePassword, which are not DependencyProperties and thus we cannot use Bindings here and you won't get any change notification for them. According to Microsoft this is because of security reasons. 

One workaround is that you can either add an Extension Method where you implement a few DP's to enable binding, or you can add a Behavior class and use Microsoft.Interactive.Behaviors. A third option could be to use an EventTrigger which calls a SetPasswordStatus command in the ViewModel. In my case I added a Behavior class, PasswordBoxBehavior.cs, but I have also implemented the ExtensionMethod approach, and the EventTrigger approach just to demonstrate, although they are commented out in favor for the Behavior approach. I'm sure there are other ways as well to go here. 

Information for the above suggested PasswordBox implementations was found here:

- https://stackoverflow.com/a/38610319/4837902 (Extension Method)
- https://stackoverflow.com/a/23057571/4837902 (Behavior)

Information on how to use Behaviors you can find here:

- https://stackoverflow.com/a/61547718/4837902 
