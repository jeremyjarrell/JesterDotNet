using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using JesterDotNet.Presenter;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JesterDotNet.Controls
{
    public partial class AssemblyGraphTreeView : UserControl
    {
        private readonly string Assembly = "imgAssembly";
        private readonly string Branch = "imgBranch";
        private readonly string Class = "imgClass";
        private readonly string Method = "imgMethod";
        private readonly string Module = "imgModule";
        private readonly string Interface = "imgInterface";

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyGraphTreeView"/> class.
        /// </summary>
        public AssemblyGraphTreeView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets an enumeration of the conditionals that the user has selected in the control.
        /// </summary>
        /// <value>an enumeration of the conditionals that the user has selected in the control.</value>
        public IList<ConditionalDefinitionDto> SelectedConditionals
        {
            get
            {
                // Get all checked tree nodes recursively
                // Return a collection of the ones that are conditionals
                IList<ConditionalDefinitionDto> selectedConditionals =
                    new List<ConditionalDefinitionDto>();
                foreach (TreeNode node in treeView.Nodes)
                {
                    foreach (TreeNode selectedNode in GetSelectedSubNodes(node))
                    {
                        // selected conditionals add stuff
                        ConditionalDefinitionDto conditionalDefinition =
                            selectedNode.Tag as ConditionalDefinitionDto;
                        if (conditionalDefinition != null)
                            selectedConditionals.Add((ConditionalDefinitionDto)selectedNode.Tag);
                    }
                }
                return selectedConditionals;
            }
        }

        private event EventHandler<EventArgs> _nodeSelectionChanged;
        public event EventHandler<EventArgs> NodeSelectionChanged
        {
            add { _nodeSelectionChanged += value; }
            remove { _nodeSelectionChanged -= value; }
        }

        /// <summary>
        /// Reflects through the currently selected assembly and reflects the type tree
        /// in tvAssemblyGraph.
        /// </summary>
        public void LoadAssemblies(string[] fileNames)
        {
            BranchingOpCodes opCodes = new BranchingOpCodes();
            treeView.Nodes.Clear();

            for (int theAssembly = 0; theAssembly < fileNames.Length; theAssembly++)
            {
                // Load our input assembly and create its node in the tree
                AssemblyDefinition inputAssembly =
                    AssemblyFactory.GetAssembly(fileNames[theAssembly]);

                TreeNode assemblyTreeNode =
                    CreateTreeNode(fileNames[theAssembly], inputAssembly, Assembly);
                treeView.Nodes.Add(assemblyTreeNode);

                // Retrieve the modules from the assembly.  Most assemblies only have one
                // module, but it is possible for assemblies to possess multiple modules
                for (int theModule = 0; theModule < inputAssembly.Modules.Count; theModule++)
                {
                    // Add a node to the tree to represent the module
                    TreeNode moduleTreeNode =
                        CreateTreeNode(inputAssembly.Modules[theModule].Name,
                                       inputAssembly.Modules[theModule], Module);
                    treeView.Nodes[theAssembly].Nodes.Add(moduleTreeNode);

                    // Add the classes in each type
                    for (int theType = 0;
                         theType < inputAssembly.Modules[theModule].Types.Count;
                         theType++)
                    {
                        // Add a node to the tree to represent the class
                        treeView.Nodes[theAssembly].Nodes[theModule].Nodes.Add(
                            CreateTreeNode(inputAssembly.Modules[theModule].Types[theType].FullName,
                                inputAssembly.Modules[theModule].Types[theType], Class));

                        // Create a test method for each method in this type
                        for (int theMethod = 0;
                             theMethod <
                             inputAssembly.Modules[theModule].Types[theType].Methods.Count;
                             theMethod++)
                        {
                            MethodDefinition methodDefinition =
                                inputAssembly.Modules[theModule].Types[theType].Methods[theMethod];
                            treeView.Nodes[theAssembly].Nodes[theModule].Nodes[theType].Nodes.Add(
                                CreateTreeNode(methodDefinition.Name, methodDefinition, Method));

                            // Store the method's MethodInfo object in this node's tag
                            // so that we may retrieve it later
                            treeView.Nodes[theAssembly].Nodes[theModule].Nodes[theType].Nodes[
                                theMethod].Tag =
                                methodDefinition;

                            // Interfaces or abstract classes don't have a body so we should skip 
                            // them
                            if (methodDefinition.HasBody)
                            {
                                MethodBody body = methodDefinition.Body;
                                for (int theInstruction = 0;
                                     theInstruction < body.Instructions.Count;
                                     theInstruction++)
                                {
                                    if (opCodes.ContainsKey(
                                        body.Instructions[theInstruction].OpCode))
                                    {
                                        treeView.
                                            Nodes[theAssembly].
                                            Nodes[theModule].
                                            Nodes[theType].
                                            Nodes[theMethod].
                                            Nodes.Add(
                                            CreateTreeNode(
                                                opCodes[body.Instructions[theInstruction].OpCode].ToString(),
                                                new ConditionalDefinitionDto(methodDefinition, theInstruction), Branch));
                                    }
                                }
                            }
                        }
                    }
                    moduleTreeNode.Expand();
                }
                assemblyTreeNode.Expand();
            }
        }

        private IEnumerable<TreeNode> GetSelectedSubNodes(TreeNode parentNode)
        {
            List<TreeNode> selectedNodes = new List<TreeNode>();
            foreach (TreeNode childNode in parentNode.Nodes)
            {
                if (childNode.Checked)
                {
                    selectedNodes.Add(childNode);
                    selectedNodes.AddRange(GetSelectedSubNodes(childNode));
                }
            }
            return selectedNodes;
        }

        /// <summary>
        /// Applies the check state of the given 
        /// <see cref="System.Windows.Forms.TreeNode">TreeNode</see> to all of its 
        /// children.
        /// </summary>
        /// <param name="treeNode">The TreeNode which contains the check state to apply.</param>
        private static void CheckChildren(TreeNode treeNode)
        {
            if (treeNode.Nodes.Count != 0)
            {
                CheckChildren(treeNode.Nodes[0]);
                for (int i = 0; i < treeNode.Nodes.Count; i++)
                {
                    treeNode.Nodes[i].Checked = treeNode.Checked;
                }
            }
            // TODO: We also need to check the parents (or graystate them if not all of their children are checked)
        }

        /// <summary>
        /// Creates a <see cref="System.Windows.Forms.TreeNode">TreeNode</see> with the 
        /// given text and image key.
        /// </summary>
        /// <param name="text">The text of the TreeNode.</param>
        /// <param name="memberDefinition">The definition of the member that this node represents.</param>
        /// <param name="imageKey">The key corresponding to the TreeNode's image.</param>
        /// <returns>A valid <see cref="TreeNode"/> representing the given attributes.</returns>
        private TreeNode CreateTreeNode(string text, object memberDefinition, string imageKey)
        {
            TreeNode treeNode = new TreeNode(text);
            treeNode.Tag = memberDefinition;

            treeNode.Checked = !(IsInterface(memberDefinition));
            treeNode.Checked = !(IsAbstractMethod(memberDefinition));
            if (IsInterface(memberDefinition) || IsAbstractMethod(memberDefinition))
            {
                treeNode.Checked = false;
                treeNode.NodeFont = new Font(treeView.Font, FontStyle.Italic);
                treeNode.ForeColor = SystemColors.GrayText;
                treeNode.ImageIndex = objectIconsImageList.Images.IndexOfKey(Interface);
                treeNode.SelectedImageIndex = objectIconsImageList.Images.IndexOfKey(Interface);
            }
            else
            {
                treeNode.Checked = true;
                treeNode.ImageIndex = objectIconsImageList.Images.IndexOfKey(imageKey);
                treeNode.SelectedImageIndex = objectIconsImageList.Images.IndexOfKey(imageKey);
            }

            return treeNode;
        }

        /// <summary>
        /// Called when the user checks a node of the tree view.  Propogates the check state down
        /// to all of the little check children.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="TreeViewEventArgs"/> instance containing the event 
        /// data.</param>
        private void OnTreeViewAfterCheck(object sender, TreeViewEventArgs e)
        {
            CheckChildren(e.Node);
        }

        /// <summary>
        /// Handles the BeforeCheck event of the treeView control.  Ensures that the selected node is a valid type of
        /// node, i.e., not an interface or abstract method, that can be checked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TreeViewCancelEventArgs"/> instance containing the event data.</param>
        private void treeView_BeforeCheck(object sender, TreeViewCancelEventArgs e)
        {
            if (IsInterface(e.Node.Tag) || IsAbstractMethod(e.Node.Tag))
                e.Cancel = true;
        }

        /// <summary>
        /// Determines whether the specified member definition is interface.
        /// </summary>
        /// <param name="memberDefinition">The member definition to verify.</param>
        /// <returns>
        /// <c>true</c> if the specified member definition is interface; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsInterface(object memberDefinition)
        {
            TypeDefinition typeDefinition = memberDefinition as TypeDefinition;
            if (typeDefinition != null)
                if (typeDefinition.IsInterface)
                    return true;

            return false;
        }

        /// <summary>
        /// Determines whether the specified member definition is an abstract method.
        /// </summary>
        /// <param name="memberDefinition">The member definition to verify.</param>
        /// <returns>
        /// <c>true</c> if the specified member definition is an abstract method; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsAbstractMethod(object memberDefinition)
        {
            MethodDefinition methodDefinition = memberDefinition as MethodDefinition;
            if (methodDefinition != null)
                if (!(methodDefinition.HasBody))
                    return true;

            return false;
        }

        public void HighlightCorrespondingMember(MethodDefinition associatedMethod)
        {
            foreach (TreeNode node in treeView.Nodes)
            {
                TreeNode matchingNode = GetCorrespondingNode(node);
                if (matchingNode != null)
                    matchingNode.EnsureVisible();
            }
        }

        private TreeNode GetCorrespondingNode(TreeNode node)
        {
            MethodDefinition methodDefinition = node.Tag as MethodDefinition;
            if (methodDefinition != null)
                if (methodDefinition.ToString() == methodDefinition.ToString())
                    return node;
            return null;
        }
    }
}