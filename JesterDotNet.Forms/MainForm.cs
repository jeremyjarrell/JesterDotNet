using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using JesterDotNet.Presenter;
using JesterDotNet.UI.Utility;

namespace JesterDotNet.Forms
{
    /// <summary>
    /// Acts as the main UI for the end user.
    /// </summary>
    public partial class MainForm : Form, IJesterView
    {
        private readonly BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private string _shadowedTargetAssembly;
        private string _shadowedTestAssembly;

        #region Constructors (Public)

        /// <summary>
        /// Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            var presenter = new JesterPresenter(this);
            presenter.TestComplete += presenter_TestComplete;
            presenter.MutationComplete += presenter_MutationComplete;
        }

        #endregion Constructors (Public)

        #region IJesterView Members

        /// <summary>
        /// Occurs when the user has initiated the full mutation and test process.
        /// </summary>
        public event EventHandler<RunEventArgs> Run;

        /// <summary>
        /// Gets a value indicating whether user has cancelled the mutation.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the user has cancelled the mutation; otherwise, <c>false</c>.
        /// </value>
        public bool CancellationPending
        {
            get { return _backgroundWorker.CancellationPending; }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Click event of the openToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the 
        /// event data.</param>
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (projectOpenFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                foreach (string fileName in projectOpenFileDialog.FileNames)
                {
                    LoadProjectFile(fileName);
                }
                runButton.Enabled = true;
            }
        }

        /// <summary>
        /// Handles the MutationComplete event of the presenter control.  Populates the list 
        /// view with the results of the mutation test.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MutationCompleteEventArgs"/> instance containing 
        /// the event data.</param>
        private void presenter_MutationComplete(object sender, MutationCompleteEventArgs e)
        {
            foreach (MutationDto mutation in e.MutationResults)
            {
                foreach (TestResultDto result in mutation.TestResults)
                {
                    var killedMutantTestResultDto = result as KilledMutantTestResultDto;
                    if (killedMutantTestResultDto != null)
                    {
                        if (mutationErrorsListView.InvokeRequired)
                        {
                            mutationErrorsListView.Invoke(
                                (MethodInvoker) delegate
                                                    {
                                                        mutationErrorsListView.Items.Add("", 0);
                                                        mutationErrorsListView.Items[
                                                            mutationErrorsListView.Items.Count - 1].
                                                            SubItems.Add(killedMutantTestResultDto.Name);
                                                        mutationErrorsListView.Items[
                                                            mutationErrorsListView.Items.Count - 1].
                                                            SubItems.Add(killedMutantTestResultDto.Exception);
                                                        mutationErrorsListView.Items[
                                                            mutationErrorsListView.Items.Count - 1].
                                                            SubItems.Add(killedMutantTestResultDto.Message);
                                                        mutationErrorsListView.Items[
                                                            mutationErrorsListView.Items.Count - 1].Tag =
                                                            killedMutantTestResultDto;
                                                    });
                        }
                        else
                        {
                            mutationErrorsListView.Items.Add("", 0);
                            mutationErrorsListView.Items[mutationErrorsListView.Items.Count - 1].
                                SubItems.Add(killedMutantTestResultDto.Name);
                            mutationErrorsListView.Items[mutationErrorsListView.Items.Count - 1].
                                SubItems.Add(killedMutantTestResultDto.Exception);
                            mutationErrorsListView.Items[mutationErrorsListView.Items.Count - 1].
                                SubItems.Add(killedMutantTestResultDto.Message);
                            mutationErrorsListView.Items[mutationErrorsListView.Items.Count - 1].Tag =
                                killedMutantTestResultDto;
                        }
                    }
                    else
                    {
                        var survivingMutantTestResult = result as SurvivingMutantTestResultDto;
                        if (survivingMutantTestResult != null)
                        {
                            if (mutationErrorsListView.InvokeRequired)
                            {
                                mutationErrorsListView.Invoke(
                                    (MethodInvoker) delegate
                                                        {
                                                            mutationErrorsListView.Items.Add("", 1);
                                                            mutationErrorsListView.Items[
                                                                mutationErrorsListView.Items.Count - 1].
                                                                SubItems.Add(survivingMutantTestResult.Name);
                                                            mutationErrorsListView.Items[
                                                                mutationErrorsListView.Items.Count - 1].Tag =
                                                                survivingMutantTestResult;
                                                        });
                            }
                            else
                            {
                                mutationErrorsListView.Items.Add("", 1);
                                mutationErrorsListView.Items[mutationErrorsListView.Items.Count - 1].
                                    SubItems.Add(survivingMutantTestResult.Name);
                                mutationErrorsListView.Items[mutationErrorsListView.Items.Count - 1].Tag =
                                    survivingMutantTestResult;
                            }
                        }
                    }
                }
                if (mutationErrorsListView.InvokeRequired)
                {
                    mutationErrorsListView.Invoke(
                        (MethodInvoker) delegate
                                            {
                                                foreach (ColumnHeader columnHeader in mutationErrorsListView.Columns)
                                                    columnHeader.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                                            });
                }
                else
                {
                    foreach (ColumnHeader columnHeader in mutationErrorsListView.Columns)
                        columnHeader.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                }
            }
            if (cancelButton.InvokeRequired)
                cancelButton.Invoke((MethodInvoker) delegate { cancelButton.Enabled = false; });
            else
                cancelButton.Enabled = false;

            if (runButton.InvokeRequired)
                runButton.Invoke((MethodInvoker) delegate { runButton.Enabled = true; });
            else
                runButton.Enabled = true;
        }

        private void presenter_TestComplete(object sender, TestCompleteEventArgs e)
        {
            if (progressBar.InvokeRequired)
                progressBar.Invoke((MethodInvoker) delegate { progressBar.Value++; });
        }

        /// <summary>
        /// Handles the Click event of the RunButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the 
        /// event data.</param>
        private void RunButton_Click(object sender, EventArgs e)
        {
            CreateAndTriggerRunEvent(null, new DoWorkEventArgs(null));
        }

        /// <summary>
        /// Handles the Click event of the newToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new NewProjectFileForm())
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    LoadProjectFile(form.ProjectFilePath);
                }
                runButton.Enabled = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the optionsToolStripMenuItem control.  Displays the
        /// <see cref="PreferencesForm"/>.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the 
        /// event data.</param>
        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var preferencesForm = new PreferencesForm())
            {
                preferencesForm.ShowDialog(this);
            }
        }

        /// <summary>
        /// Handles the Click event of the aboutToolStripMenuItem control.  Displays the
        /// <see cref="AboutBoxForm"/>.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the 
        /// event data.</param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var aboutBoxForm = new AboutBoxForm())
            {
                aboutBoxForm.ShowDialog(this);
            }
        }

        #endregion Event Handlers

        /// <summary>
        /// Reconstitutes the project file from the given path and populates the 
        /// application with the referenced items. 
        /// </summary>
        /// <param name="fileName">The path of the project containing the desired 
        /// project.</param>
        private void LoadProjectFile(string fileName)
        {
            var serializer = new JesterProjectSerializer();
            JesterProject project = serializer.Deserialize(fileName);

            targetAssemblyTreeView.LoadAssemblies(new[] {project.TargetAssemblyPath});

            _shadowedTargetAssembly = Utilities.ShadowCopyAssembly(project.TargetAssemblyPath);
            _shadowedTestAssembly = Utilities.ShadowCopyAssembly(project.TestAssemblyPath);
        }

        private void ClearProgressBar()
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke((MethodInvoker)
                                   delegate
                                       {
                                           progressBar.Value = 0;
                                           progressBar.Maximum = targetAssemblyTreeView.SelectedConditionals.Count;
                                       });
            }
            else
            {
                progressBar.Value = 0;
                progressBar.Maximum = targetAssemblyTreeView.SelectedConditionals.Count;
            }
        }

        private void CreateAndTriggerRunEvent(object sender, DoWorkEventArgs e)
        {
            ClearProgressBar();
            mutationErrorsListView.Items.Clear();

            bool locked = true;
            SetUILock(locked);

            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += worker_DoWork;

            //_backgroundWorker.RunWorkerAsync();
            worker_DoWork(null, null);
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            IList<MutationDto> mutations = new List<MutationDto>();
            foreach (ConditionalDefinitionDto conditional in targetAssemblyTreeView.SelectedConditionals)
                mutations.Add(new MutationDto(conditional, new List<TestResultDto>()));

            if (Run != null)
                Run(this, new RunEventArgs(_shadowedTargetAssembly, _shadowedTestAssembly, mutations));
        }

        private void SetUILock(bool locked)
        {
            if (runButton.InvokeRequired)
                runButton.Invoke((MethodInvoker) delegate { runButton.Enabled = !(locked); });
            else
                runButton.Enabled = !(locked);
        }

        private void mutationErrorsListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mutationErrorsListView.SelectedItems.Count == 0)
                return;

            var killedMutant = mutationErrorsListView.SelectedItems[0].Tag as KilledMutantTestResultDto;
            if (killedMutant != null)
            {
                targetAssemblyTreeView.HighlightCorrespondingMember(killedMutant.ConditionalDefinition.MethodDefinition);
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            _backgroundWorker.CancelAsync();
        }

        private void mutationErrorsListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            mutationErrorsListView.Sort();
        }
    }
}