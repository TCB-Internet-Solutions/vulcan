namespace TcbInternetSolutions.Vulcan.Core
{
    internal class _TODOs
    {
        // TODONE: Fix assembly scanning error for iModifiedTrackable.IsModified on Alloy Blocks, mapping issue is field name with "."
        // TODONE: Add attribute to extract contentarea contents and use the CmsIndexModifier to look for properties with it to index
        // TODO: Add content events to work with content area attribute to sync block changes that are searchable.
        // TODONE: Figure out permissions, need to index roles with read
        // TODONE: Build read permission filter for searching
        // TODONE: Figure out how to handle block only searches when there is an IContent type restriction, just needs an OR filter for all types deriving from BlockData
        // TODO: (WIP) need help in searching... Figure out how to index media file contents for things like pdf/doc(x) etc. (elastic needs mapper-attachments installed), seems to store base64 blob as well.
        // TODO: Create simple search extension for site search queries that take search string and look for pages and files, with parameter to filter on search roots, and exclude certain types
        // TODO: Commerce search provider for UI
        // TODONE: IVulcanClient.Search needs to support blocks, multiple search roots, etc (done by adding TypeFilter with extension to get all types of BlockData).
        // TODO: Learn more about elastic 2.x setup and hosting best practices
        // TODONE: Make all assembly scanning calls use structuremap instead. (used dictionary to store assembly scan, and methods to create search clients)
        // TODO: Research if index job can be batched similar to Find.
        // TODO: Add indexer to allow for custom objects that doen't inherit IContent. And Search function in the vuclan client to handle.
    }
}
