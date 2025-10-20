
using System;
using Newtonsoft.Json;

namespace Updater.Models;

public class UpdateConfig
{
    [JsonProperty("softwareName")]
    public string SoftwareName { get; set; } = "My Application";
        
    [JsonProperty("currentVersion")]
    public string CurrentVersion { get; set; } = "1.0.0";
        
    [JsonProperty("githubRepo")]
    public string GithubRepo { get; set; } = "owner/repository";
        
    [JsonProperty("githubToken")]
    public string GithubToken { get; set; } = "";
        
    [JsonProperty("autoCheck")]
    public bool AutoCheck { get; set; } = true;
        
    [JsonProperty("downloadPath")]
    public string DownloadPath { get; set; } = "./updates";
        
    [JsonProperty("backupBeforeUpdate")]
    public bool BackupBeforeUpdate { get; set; } = true;
        
    [JsonProperty("targetExecutable")]
    public string TargetExecutable { get; set; } = "MyApp.exe";
}