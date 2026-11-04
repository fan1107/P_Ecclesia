"""Import official unitypackage files without launching a second interactive editor."""
import pathlib, tarfile, shutil
root = pathlib.Path(__file__).resolve().parent.parent
package = root / 'Tools/firebase_unity_sdk/FirebaseAuth.unitypackage'
with tarfile.open(package, 'r:gz') as archive:
    members = {m.name: m for m in archive.getmembers()}
    count = 0
    for name, member in members.items():
        if not name.endswith('/pathname'): continue
        path = archive.extractfile(member).read().decode().strip()
        destination = (root / path).resolve()
        if not destination.is_relative_to(root / 'Assets'): raise ValueError(path)
        prefix = name.rsplit('/', 1)[0]
        asset = members.get(prefix + '/asset')
        if asset:
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(archive.extractfile(asset).read())
        else: destination.mkdir(parents=True, exist_ok=True)
        meta = members.get(prefix + '/asset.meta')
        if meta: pathlib.Path(str(destination)+'.meta').write_bytes(archive.extractfile(meta).read())
        count += 1
    print(f'Imported {count} Firebase assets')
source = root / 'Tools/GoogleSource/siwg-mobile-unity-plugin-main'
target = root / 'Assets/GoogleSignInNative'
for folder in ['Runtime', 'Editor', 'Resources']:
    shutil.copytree(source / folder, target / folder, dirs_exist_ok=True)
shutil.copy2(source/'LICENSE', target/'LICENSE.txt')
android = root / 'Assets/Plugins/Android'
android.mkdir(parents=True, exist_ok=True)
shutil.copytree(source/'Plugins/Android/GoogleSignInNativeAndroid.androidlib', android/'GoogleSignInNativeAndroid.androidlib', dirs_exist_ok=True)
shutil.copy2(source/'Plugins/Android/GoogleSignInNativeAndroid.androidlib.meta', android)
print('Imported Google sign-in runtime, settings and Android library')
