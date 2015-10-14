Imports NAppUpdate.Framework
Imports NAppUpdate.Framework.Common
Imports NAppUpdate.Framework.Sources

Public Class frmUpdate
    Private Async Sub frmUpdate_Shown(sender As Object, e As EventArgs) Handles Me.Shown

        Await CheckUpdateStatus()


    End Sub
    Sub Log(Message As String)
        txtLog.AppendText(Message)
        txtLog.AppendText(Environment.NewLine)


    End Sub


    Function CheckUpdateStatus() As Task
        Dim tsUi As TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext

        Dim tsk As Task
        If Debugger.IsAttached Then
            tsk = New Task(
                Sub()
                    Invoke(Sub()
                               'I know this is silly just to log a message 
                               'but we need to run without debugging
                               Log("Debugger is attached.  Run with ctrl-F5 to test update functionality.")
                           End Sub)
                End Sub)
        Else

            'todo make a new version by 
            'Build the NAppUpdate.Updater project first then build NAppUpdate.Framework.  
            '1.  Make a new version of this program by changing the version info to 2.0 and change color of form and Build.
            '2.  Undo your changes to put it back to 1.0. DO NOT build yet.  
            '3.  Run the feedbuilder tool using the provided feedbuilder config file.  
            '       Adjust the output directory and base url and project output to match where the project is located
            '4.  Click refresh files.
            '5.  Click build in feedbuilder.  You should have your files in the updates folder
            '6.  Undo your changes to make it version 1.  
            '7.  Press ctrl-F5.  

            'see ApplicationEvents.vb for startup config
            tsk = New Task(Sub()
                               Dim uiTf As New TaskFactory(tsUi)

                               Dim updateMgr As UpdateManager = UpdateManager.Instance

                               Dim pth As String = IO.Path.GetFullPath(IO.Path.Combine(My.Application.Info.DirectoryPath, "..\updates"))
                               updateMgr.UpdateFeedReader = New FeedReaders.NauXmlFeedReader
                               'relative path from where the program is to make it easier to test.
                               updateMgr.UpdateSource = New SimpleWebSource(
                               String.Format("file:///{0}", IO.Path.Combine(pth, "updates.xml").Replace("\", "/")))





                               AddHandler updateMgr.ReportProgress, AddressOf udm_ReportProgress
                               Select Case updateMgr.State
                                   Case UpdateManager.UpdateProcessState.NotChecked
                                       Try
                                           uiTf.StartNew(Sub()
                                                             Log("Checking for updates...")
                                                         End Sub)
                                           updateMgr.CheckForUpdates()
                                           If updateMgr.UpdatesAvailable > 0 Then
                                               uiTf.StartNew(Sub()
                                                                 Log("Preparing updates...")
                                                             End Sub)

                                               updateMgr.PrepareUpdates()
                                               uiTf.StartNew(Sub()
                                                                 Log("Applying updates...")
                                                             End Sub)
                                               Threading.Thread.Sleep(3000)
                                               updateMgr.ApplyUpdates(True, True, False)

                                           Else
                                               uiTf.StartNew(Sub()
                                                                 Log("No Updates")

                                                             End Sub)
                                           End If

                                       Catch ex As Exception
                                           updateMgr.Logger.Log(ex)
                                           uiTf.StartNew(Sub()
                                                             Dim msg As String = ex.Message
                                                             While ex.InnerException IsNot Nothing
                                                                 msg &= Environment.NewLine & ex.InnerException.Message
                                                             End While
                                                             Log(msg)
                                                         End Sub)
                                           Try

                                               If updateMgr.State = UpdateManager.UpdateProcessState.RollbackRequired Then
                                                   uiTf.StartNew(Sub()
                                                                     Log("Rolling back updates...")
                                                                 End Sub)
                                                   updateMgr.RollbackUpdates()

                                               End If

                                           Catch exInner As Exception
                                               updateMgr.Logger.Log(exInner)
                                               uiTf.StartNew(Sub()
                                                                 Log(String.Format("Failed rollback.  {0}", exInner.Message))
                                                             End Sub)

                                           End Try

                                       Finally
                                           If updateMgr.State <> UpdateManager.UpdateProcessState.AppliedSuccessfully Then updateMgr.CleanUp()
                                       End Try
                                   Case UpdateManager.UpdateProcessState.AfterRestart

                                       uiTf.StartNew(Sub()
                                                         Log("Updates Applied")
                                                         Log(String.Format("Update Info: New version:{0} Updates:{1}", My.Application.Info.Version.ToString, updateMgr.Tasks.Count()))

                                                     End Sub)
                               End Select
                               RemoveHandler updateMgr.ReportProgress, AddressOf udm_ReportProgress

                           End Sub)

        End If

        tsk.Start()
        Return tsk

    End Function

    Private Sub udm_ReportProgress(currentStatus As UpdateProgressInfo)
        Invoke(Sub()
                   Log(String.Format("App update progress:{0}", currentStatus.Message))
               End Sub)

    End Sub
End Class
