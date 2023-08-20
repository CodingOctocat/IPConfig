# IPConfig

[![Target framework](https://img.shields.io/badge/support-.NET_7.0--Windows-blue)](https://github.com/CodingOctocat/IPConfig)
[![GitHub issues](https://img.shields.io/github/issues/CodingOctocat/IPConfig)](https://github.com/CodingOctocat/IPConfig/issues)
[![GitHub stars](https://img.shields.io/github/stars/CodingOctocat/IPConfig)](https://github.com/CodingOctocat/IPConfig/stargazers)
[![GitHub license](https://img.shields.io/github/license/CodingOctocat/IPConfig)](https://github.com/CodingOctocat/IPConfig/blob/master/LICENSE)
[![CodeFactor](https://www.codefactor.io/repository/github/codingoctocat/ipconfig/badge)](https://www.codefactor.io/repository/github/codingoctocat/ipconfig)

A simple and easy IP configuration tool.

<a href="https://github.com/CodingOctocat/IPConfig">
    <img src="/IPConfig/Resources/network-tree.png" alt="Logo" width="128">
</a>

---

## Features

- Simple, easy, forget the network settings portal in the Control Panel <img src="/IPConfig/Resources/shell32.dll(22).png" alt="Control Panel" height="16">
- Switching between your preset IP configurations
- Support for viewing network adapter details
- Customizable subnet mask and DNS preset list
- Batch ping DNS list
- Light, Dark, Violet themes
- ...

## Screenshots

<img src="/IPConfig/Resources/Screenshots/mainwindow.png" alt="mainwindow" width="484">

## Languages

- English - en
- 中文 (中国) - zh-CN

## Shortcuts

- `F2`: Rename configuration
- `F5`: Refresh network adapter list
- `F9`: Focus on configuration list
- `F11`: View to network adapter IP configuration
- `F12`: Navigate to the network adapter details view
- `Ctrl+F`: Search configuration
- `Ctrl+S`: Save configuration

## FAQ

1. How to customise subnet mask and DNS preset list?
    > You can create a copy of the template and edit it, while following to file naming convention: `filename[.lang].csv` (Don't use English periods(`.`) in filename).
    For example, `ipv4_public_dns.fr.csv` is good, `ipv4.public.dns.fr.csv` is bad.
