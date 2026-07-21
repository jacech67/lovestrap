> [!CAUTION]
> The only official places to download Lovestrap are this GitHub repository and [discord.gg/soarr](discord.gg/soarr). Any other websites offering downloads or claiming to be us are not owned by us.

> [!IMPORTANT]
> Lovestrap is not under active development anymore. Any issues or pull requests opened may take a while to get a response.

<p align="center">
    <img src="Images/Lovestrap-full-dark.png#gh-dark-mode-only" width="200">
    <img src="Images/Lovestrap-full-light.png#gh-light-mode-only" width="200">
</p>

<div align="center">

[![License][shield-repo-license]][repo-license]
[![GitHub Workflow Status][shield-repo-workflow]][repo-actions]
[![Crowdin][shield-crowdin-status]][crowdin-project]
[![Downloads][shield-repo-releases]][repo-releases]
[![Version][shield-repo-latest]][repo-latest]
[![Discord][shield-discord-server]][discord-invite]
[![lol][shield-tenor-meme]][tenor-gif]

</div>

----

Lovestrap is a third-party replacement for the standard Roblox bootstrapper, providing additional useful features and improvements.

Running into a problem or need help with something? [Check out the extras page!](discord.gg/soarr). If you can't find anything, or would like to suggest something, please [submit an issue](discord.gg/soarr).

Lovestrap is only supported for PCs running Windows.

## Frequently Asked Questions

**Q: Is this malware?**

**A:** No. The source code here is viewable to all, and it'd be impossible for us to slip anything malicious into the downloads without anyone noticing. Just be sure you're downloading it from an official source. The only two official sources are this GitHub repository and [discord.gg/soarr](discord.gg/soarr).

**Q: Can using this get me banned?**

**A:** No, it shouldn't. Lovestrap doesn't interact with the Roblox client in the same way that exploits do. [Read more about that here.](https://bloxstraplabs.com/wiki/info/bloxstrap-and-bans)
## Features

- Hassle-free Discord Rich Presence to let your friends know what you're playing at a glance
- Simple support for modding of content files for customizability (death sound, mouse cursor, etc)
- See where your server is geographically located (courtesy of [ipinfo.io](https://ipinfo.io))
- Ability to configure graphics fidelity and UI experience

## Installing
Download the [latest release of Lovestrap](https://github.com/bloxstraplabs/bloxstrap/releases/latest), and run it. Configure your preferences if needed, and install. That's about it!

Alternatively, you can install Lovestrap via [Winget](https://winstall.app/apps/pizzaboxer.Bloxstrap) by running this in a Command Prompt window:
```
> winget install bloxstrap
```

You will also need the [.NET 6 Desktop Runtime](https://aka.ms/dotnet-core-applaunch?missing_runtime=true&arch=x64&rid=win11-x64&apphost_version=6.0.36&gui=true). If you don't already have it installed, you'll be prompted to install it anyway. Be sure to install Lovestrap after you've installed this.

It's not unlikely that Windows Smartscreen will show a popup when you run Lovestrap for the first time. This happens because it's an unknown program, not because it's actually detected as being malicious. To dismiss it, just click on "More info" and then "Run anyway".

Once installed, Lovestrap is added to your Start Menu, where you can access the menu and reconfigure your preferences if needed.

## Code

Lovestrap uses the [WPF UI](https://github.com/lepoco/wpfui) library for the user interface design. We currently use and maintain our own fork of WPF UI at [bloxstraplabs/wpfui](https://github.com/bloxstraplabs/wpfui).


[shield-repo-license]:  https://img.shields.io/github/license/bloxstraplabs/bloxstrap
[shield-repo-workflow]: https://img.shields.io/github/actions/workflow/status/bloxstraplabs/bloxstrap/ci-release.yml?branch=main&label=builds
[shield-repo-releases]: https://img.shields.io/github/downloads/bloxstraplabs/bloxstrap/latest/total?color=981bfe
[shield-repo-latest]:   https://img.shields.io/github/v/release/bloxstraplabs/bloxstrap?color=7a39fb

[shield-crowdin-status]: https://badges.crowdin.net/bloxstrap/localized.svg
[shield-discord-server]: https://img.shields.io/discord/1099468797410283540?logo=discord&logoColor=white&label=discord&color=4d3dff
[shield-tenor-meme]:     https://img.shields.io/badge/mom_made-pizza_rolls-orange

[repo-license]:  https://github.com/bloxstraplabs/bloxstrap/blob/main/LICENSE
[repo-actions]:  https://github.com/bloxstraplabs/bloxstrap/actions
[repo-releases]: https://github.com/bloxstraplabs/bloxstrap/releases
[repo-latest]:   https://github.com/bloxstraplabs/bloxstrap/releases/latest

[crowdin-project]: https://crowdin.com/project/bloxstrap
[discord-invite]:  https://discord.gg/nKjV3mGq6R
[tenor-gif]:       https://media.tenor.com/FIkSGbGycmAAAAAd/manly-roblox.gif

## Code signing policy

Thanks to [SignPath.io](https://signpath.io/) for providing a free code signing service, and the [SignPath Foundation](https://signpath.org/) for providing the free code signing certificate.
