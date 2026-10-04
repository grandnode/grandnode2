using Grand.Module.Installer.Services.MessageTemplates;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual Task InstallMessageTemplates()
    {
        var eaGeneral = _emailAccountRepository.Table.FirstOrDefault();
        if (eaGeneral == null)
            throw new Exception("Default email account cannot be loaded");

        MessageTemplateSeed.Build(eaGeneral.Id).ForEach(x => _messageTemplateRepository.Insert(x));
        return Task.CompletedTask;
    }
}
