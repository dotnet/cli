namespace Microsoft.DotNet.ProjectModel.Graph
{
    public interface IMergeable<T>
    {
        void MergeWith(T m);
    }
}