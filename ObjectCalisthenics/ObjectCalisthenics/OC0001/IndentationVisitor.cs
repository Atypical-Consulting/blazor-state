namespace ObjectCalisthenics;

internal class IndentationVisitor : CSharpSyntaxWalker
{
    private int _currentDepth;
    public bool HasMultipleLevelsOfIndentation { get; private set; }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Ensure we're only checking the method body
        if (node.Body != null)
        {
            Visit(node.Body);
        }
    }

    public override void VisitBlock(BlockSyntax node)
    {
        // Increment depth only if the block is not the first method block.
        if (node.Parent is not MethodDeclarationSyntax)
        {
            _currentDepth++;
        }

        if (_currentDepth > 1)
        {
            HasMultipleLevelsOfIndentation = true;
        }

        base.VisitBlock(node);

        // Decrement depth only if we previously incremented it.
        if (node.Parent is not MethodDeclarationSyntax)
        {
            _currentDepth--;
        }
    }
}