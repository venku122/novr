"""Exercise the real MSBuild stage/deploy targets against a disposable game tree."""
import argparse
from pathlib import Path
import subprocess
import tempfile
import json
import xml.sax.saxutils

parser = argparse.ArgumentParser()
parser.add_argument('--dotnet', required=True)
args = parser.parse_args()
targets = Path(__file__).resolve().parents[1] / 'NOVR.Build/NOVR.Build.targets'
with tempfile.TemporaryDirectory() as directory:
    root = Path(directory)
    output = root / 'output'
    output.mkdir()
    (output / 'NOVR.dll').write_text('fixture')
    project = root / 'fixture.proj'
    project.write_text(f'''<Project DefaultTargets="Build">
      <PropertyGroup>
        <TargetDir>{output}/</TargetDir>
        <GameDeployPath>BepInEx/plugins/NOVR</GameDeployPath>
        <GameLayoutOutputDir>{root}/stage</GameLayoutOutputDir>
        <NuclearOptionGameDirResolved>{root}/live</NuclearOptionGameDirResolved>
      </PropertyGroup>
      <Import Project="{xml.sax.saxutils.escape(str(targets))}" />
      <Target Name="Build" />
    </Project>''')
    subprocess.run([args.dotnet, 'msbuild', str(project), '-p:NovrAutoDeploy=false', '-v:q', '-nologo'], check=True)
    assert (root / 'stage/BepInEx/plugins/NOVR/NOVR.dll').read_text() == 'fixture', 'Build must stage its payload'
    assert not (root / 'live').exists(), 'Safe build must not create or write a live installation'
    subprocess.run([args.dotnet, 'msbuild', str(project), '-p:NovrAutoDeploy=true', '-v:q', '-nologo'], check=True)
    assert (root / 'live/BepInEx/plugins/NOVR/NOVR.dll').read_text() == 'fixture', 'Legacy opt-in remains compatible'
    # Relocated intermediates must not pull an older platform's obj sources into compilation.
    stale = root / 'obj/Release'
    stale.mkdir(parents=True)
    (stale / 'stale.AssemblyAttributes.cs').write_text('// old SDK output')
    sdk_project = root / 'glob.csproj'
    sdk_project.write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
      <Import Project="{xml.sax.saxutils.escape(str(targets.with_name('NOVR.Build.props')))}" />
      <PropertyGroup><TargetFramework>net8.0</TargetFramework><SkipNuclearOptionReferences>true</SkipNuclearOptionReferences></PropertyGroup>
    </Project>''')
    items = json.loads(subprocess.check_output([args.dotnet, 'msbuild', str(sdk_project),
        f'-p:NovrIntermediateRoot={root}/relocated', '-getItem:Compile', '-nologo'], text=True))
    assert not any('stale.AssemblyAttributes' in item['Identity'] for item in items['Items']['Compile']), 'Old obj files must stay excluded after intermediate relocation'
    subprocess.run([args.dotnet, 'msbuild', str(sdk_project), '-getItem:Compile', '-nologo'], check=True, stdout=subprocess.DEVNULL)
    print('PASS: real MSBuild staging without live writes, legacy compatibility, relocated obj exclusion, empty intermediate-root safety')
