﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BFML.Repository;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Version;
using CmlLib.Core.VersionLoader;
using Utils;

namespace BFML.Core;

internal sealed class Game
{
    internal IVersionLoader Versions => _launcher.VersionLoader;
    private readonly Repo _repo;
    private readonly CMLauncher _launcher;

    internal Game(Repo repo)
    {
        _repo = repo;
        MinecraftPath minecraftPath = new MinecraftPath(_repo.LocalPrefs.GameDirectory.FullName);
        _launcher = new CMLauncher(minecraftPath);
    }

    public async Task Launch(string nickname, GameConfiguration gameConfiguration)
    {
        Result<MVersion> preparationResult = await PrepareConfiguration(gameConfiguration, _repo.LocalPrefs.FileValidationMode);
        if (!preparationResult.IsOk) throw preparationResult.Error; 
        
        LaunchConfiguration launchConfig = new LaunchConfiguration(
            nickname, 
            preparationResult.Value,
            _repo.LocalPrefs.DedicatedRam, 
            _repo.LocalPrefs.IsFullscreen);
        
        await StartGameProcess(launchConfig);
    }

    private async Task<Result<MVersion>> PrepareConfiguration(GameConfiguration gameConfiguration, FileValidation validationMode)
    {
        Result<MVersion> vanillaPreparation = await PrepareVanilla(gameConfiguration.Vanilla.ToString());
        if (!vanillaPreparation.IsOk) return Result<MVersion>.Err(vanillaPreparation.Error);

        if (!gameConfiguration.IsModded) return Result<MVersion>.Ok(vanillaPreparation.Value);
        
        Result<MVersion> forgePreparation = await PrepareForge(gameConfiguration.Forge, validationMode);
        if (!forgePreparation.IsOk) return Result<MVersion>.Err(forgePreparation.Error);
        
        return (await PrepareMods(gameConfiguration.ModPack, validationMode))
            .Match(ok => Result<MVersion>.Ok(forgePreparation.Value), 
                err => Result<MVersion>.Err(err));
    }

    private async Task<Result<MVersion>> PrepareVanilla(string versionName)
    {
        if (string.IsNullOrWhiteSpace(versionName))
        {
            return Result<MVersion>.Err(new InvalidDataException("Version name is empty."));
        }

        try
        {
            MVersion vanilla = await _launcher.GetVersionAsync(versionName);
            await _launcher.CheckAndDownloadAsync(vanilla); // TODO track progress and time out
            return vanilla;
        }
        catch (Exception e)
        {
            return Result<MVersion>.Err(e);
        }
    }

    private async Task<Result<MVersion>> PrepareForge(Forge forge, FileValidation validationMode)
    {
        Result<bool> installation = await _repo.InstallForge(forge, validationMode);
        if (!installation.IsOk) return Result<MVersion>.Err(installation.Error);

        try
        {
            MVersionCollection localVersions = await new LocalVersionLoader(_launcher.MinecraftPath).GetVersionMetadatasAsync();
            MVersion forgeVersion = await localVersions.GetVersionAsync(forge.Name);
            return Result<MVersion>.Ok(forgeVersion);
        }
        catch (Exception e)
        {
            return Result<MVersion>.Err(e);
        }
    }

    private Task<Result<bool>> PrepareMods(ModPack modPack, FileValidation validationMode)
    {
        return _repo.InstallModPack(modPack, validationMode);
    }
    
    private async Task StartGameProcess(LaunchConfiguration launchConfig)
    {
        System.Net.ServicePointManager.DefaultConnectionLimit = 256;
        Process process = await _launcher.CreateProcessAsync(launchConfig.Version, new MLaunchOption
        {
            MaximumRamMb = launchConfig.DedicatedRam,
            Session = MSession.CreateOfflineSession(launchConfig.Nickname),
            FullScreen = launchConfig.FullScreen
        }, false);
        process.Start();
    }
}