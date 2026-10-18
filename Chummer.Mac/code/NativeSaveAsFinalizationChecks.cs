#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;
namespace Chummer;
internal static class NativeSaveAsFinalizationChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character original, string output, Action<bool,string> check)
    {
        foreach(int scenario in new[]{0,1,2,3})
        {
            var character=new Character(); character.LoadSnapshot(original.CaptureSnapshot());
            character.Created=false; character.BuildMethod=CharacterBuildMethod.BP; character.IgnoreRules=true;
            character.Nuyen=123456; character.Lifestyles.Clear(); character.FileName=Path.Combine(output,"synthetic-save-source-"+scenario+".chum");
            string destination=Path.Combine(output,"synthetic-save-result-"+scenario+".chum");
            using var editor=new frmCreate(character){Loading=true,MdiParent=owner}; editor.Show();
            var checkbox=(Forms.CheckBox)editor.GetType().GetField("chkCharacterCreated",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(editor)!;
            checkbox.Checked=true; var before=character.CaptureSnapshot();
            bool completed=false,confirmationOpened=false; int acceptedMoney=0;
            var replies=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Avalonia.Threading.Dispatcher.UIThread.Post(async ()=>{
                try
                {
                    var deadline=DateTime.UtcNow.AddSeconds(20);
                    while(!completed)
                    {
                        if(DateTime.UtcNow>=deadline)throw new TimeoutException("Synthetic creation save did not return.");
                        var message=Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().LastOrDefault();
                        if(message!=null){confirmationOpened=true;message.DialogResult=scenario==2?Forms.DialogResult.No:Forms.DialogResult.Yes;message.Close();}
                        if(NativeDialogHost.ActiveWindow is StartingMoneyWindow money)
                        { money.RollInput.Value=money.RollInput.Maximum; acceptedMoney=Math.Max(0,money.Amount); money.Accept(); }
                        await Task.Delay(10);
                    }
                    replies.TrySetResult();
                }
                catch(Exception error){NativeDialogHost.CancelAll();replies.TrySetException(error);}
            });
            try
            {
                bool saved=editor.SaveCharacterAs(blnEscapeAfterSave:scenario==1||scenario==3,chooseSavePath:()=>scenario<2?null!:destination);
                completed=true;await replies.Task;
                if(scenario<2)
                    check(!saved&&!confirmationOpened&&character.CaptureSnapshot().AsSpan().SequenceEqual(before)&&!File.Exists(character.FileName),
                        "Native Save As path cancellation preserves creation state money and lifestyles in "+(scenario==0?"direct":"escape-after-save")+" flow");
                else if(scenario==2)
                    check(!saved&&confirmationOpened&&!character.Created&&character.Nuyen==123456&&character.Lifestyles.Count==0&&!File.Exists(destination),
                        "Native chosen save path followed by creation No preserves model and writes no character");
                else
                {
                    var reopened=new Character{FileName=destination};
                    check(saved&&confirmationOpened&&character.Created&&character.Nuyen==acceptedMoney&&character.Lifestyles.Count==1&&character.FileName==destination&&
                        reopened.Load(false)&&reopened.Created&&reopened.Nuyen==acceptedMoney&&File.ReadAllBytes(destination).Take(2).SequenceEqual(new byte[]{255,254}),
                        "Native accepted Save As retains finalization destination and legacy UTF16 saved state");
                }
            }
            finally {completed=true;editor.GetType().GetField("_blnIsDirty",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(editor,false);editor.Close();}
        }
    }
}
