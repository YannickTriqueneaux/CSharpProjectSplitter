# C# Project Splitter
This is a project to display C# projects into a DOT graph (.csproj)

The tool displays the project´s folders like they were splitted in different projects.
The idea is to help identify the folders dependencies and figure a way to split your project into multiples.

It uses Rolsyn Compiler API to read you project´s files and detect depencencies between them.

After that a graph will be displayed to show you the depenencies between folders.

If you want to split a folder into multiple folders, simply click on it, and the graph will be updated.
If you want to analyze and split only one these folders Folder into multiple and ignore all others, right click on it.

You can visualize dependencies by clicking on edges.

You can visualize the code depending on, by clicking on the dependency discribed in edge content.


![image](https://user-images.githubusercontent.com/5014260/210430410-1afdd680-8da1-4a21-b4d9-a78bdba6ba3b.png)

It actually was made to support large scaled projects with 30k files+
![image](https://user-images.githubusercontent.com/5014260/208502380-3d6cb445-c623-4884-aba4-a58ed61ff6d8.png)



# How to launch it
- Make sure to install DOT by executing the installer at the root of the depot
- Ensure you chose to add the dot.exe in your PATH environement variables (an option in the installer)
- Launch Built/CSharpProjectSplitter.UI.exe

# How to change the code and compile it
- Try to clone the depot and compile the VisualSutdio solution.

I Sincerly didn´t try to make it easy for you to modify it. 
I don´t really care of this project and the way it is setup with Graphiz dependency.
Take it, brake it, modify it, sell it. I don´t really care, that´s why this depot has no particular license.
The code is ugly and WPF developers might vomit the way it is implemented.
I´m really bad in WPF, I hate it and I just did it in one weekend, so sorry if I didn´t respect MVVM and all the disgusting verbosity needed in WPF to do something clean.

Please fork and do better if you want.
