namespace Merge.Results
{
    public interface IMergeResults
    {
        void AddToMerge(string[] files);

        string[] Merge(string outputPath);
    }
}