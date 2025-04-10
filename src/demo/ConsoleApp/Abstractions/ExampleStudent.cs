// Copyright (c) 2020-2024 Atypical Consulting SRL. All rights reserved.
// Atypical Consulting SRL licenses this file to you under the Apache 2.0 license.
// See the LICENSE file in the project root for full license information.

namespace Mutty.ConsoleApp.Abstractions;

/// <summary>
/// Base class for all examples related to the Student record.
/// </summary>
public abstract class ExampleStudent : ExampleBase
{
    /// <summary>
    /// Common method to display the properties of a student.
    /// </summary>
    /// <param name="student">The student to display.</param>
    /// <param name="maxDepth">The maximum depth to display.</param>
    protected virtual void DisplayStudentTree(Student student, int maxDepth = 1)
    {
        // Create the tree root
        Tree root = new($"[bold yellow]Student: {student.Details.Name}[/]");

        // Add student details
        TreeNode detailsNode = root.AddNode("[blue]Details[/]");
        detailsNode.AddNode($"[green]Email:[/] {student.Email}");
        detailsNode.AddNode($"[green]Age:[/] {student.Details.Age}");

        // Add enrollments
        TreeNode enrollmentsNode = root.AddNode("[blue]Enrollments[/]");

        foreach (Enrollment enrollment in student.Enrollments)
        {
            AddEnrollmentNode(enrollment, enrollmentsNode, maxDepth, 1);
        }

        // Render the tree
        Write(root);
    }

    /// <summary>
    /// Adds an enrollment node to the tree.
    /// </summary>
    /// <param name="enrollment">The enrollment to add.</param>
    /// <param name="parentNode">The parent node to add the enrollment to.</param>
    /// <param name="maxDepth">The maximum depth to display.</param>
    /// <param name="currentDepth">The current depth in the tree.</param>
    protected virtual void AddEnrollmentNode(Enrollment enrollment, TreeNode parentNode, int maxDepth, int currentDepth)
    {
        TreeNode enrollmentNode = parentNode.AddNode($"[yellow]Course: {enrollment.Course.Title}[/]");
        enrollmentNode.AddNode($"[green]Enrollment Date:[/] {enrollment.EnrollmentDate.ToShortDateString()}");
        enrollmentNode.AddNode($"[green]Description:[/] {enrollment.Course.Description}");

        if (currentDepth < maxDepth)
        {
            TreeNode modulesNode = enrollmentNode.AddNode("[blue]Modules[/]");
            foreach (CourseModule module in enrollment.Course.Modules)
            {
                AddModuleNode(module, modulesNode, maxDepth, currentDepth + 1);
            }
        }
        else
        {
            enrollmentNode.AddNode($"[orange1]Modules: {enrollment.Course.Modules.Count} items[/]");
        }
    }

    /// <summary>
    /// Adds a module node to the tree.
    /// </summary>
    /// <param name="courseModule">The course module to add.</param>
    /// <param name="parentNode">The parent node to add the module to.</param>
    /// <param name="maxDepth">The maximum depth to display.</param>
    /// <param name="currentDepth">The current depth in the tree.</param>
    protected virtual void AddModuleNode(CourseModule courseModule, TreeNode parentNode, int maxDepth, int currentDepth)
    {
        TreeNode moduleNode = parentNode.AddNode($"[yellow]Module: {courseModule.Name}[/]");

        if (currentDepth < maxDepth)
        {
            TreeNode lessonsNode = moduleNode.AddNode("[blue]Lessons[/]");
            foreach (Lesson lesson in courseModule.Lessons)
            {
                AddLessonNode(lesson, lessonsNode, maxDepth, currentDepth + 1);
            }
        }
        else
        {
            moduleNode.AddNode($"[orange1]Lessons: {courseModule.Lessons.Count} items[/]");
        }
    }

    /// <summary>
    /// Adds a lesson node to the tree.
    /// </summary>
    /// <param name="lesson">The lesson to add.</param>
    /// <param name="parentNode">The parent node to add the lesson to.</param>
    /// <param name="maxDepth">The maximum depth to display.</param>
    /// <param name="currentDepth">The current depth in the tree.</param>
    protected virtual void AddLessonNode(Lesson lesson, TreeNode parentNode, int maxDepth, int currentDepth)
    {
        TreeNode lessonNode = parentNode.AddNode($"[yellow]Lesson: {lesson.Title}[/]");

        if (currentDepth >= maxDepth)
        {
            return;
        }

        lessonNode.AddNode($"[green]Content:[/] {lesson.Content}");
    }
}
