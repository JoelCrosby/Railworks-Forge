pkgname=railworks-forge-git
pkgver=0.1
pkgrel=1
epoch=
pkgdesc="Program for managing Railworks assets, scenarios and locomotives"
arch=(x86_64)
url=""
license=('GPL')
groups=()
depends=(dotnet-runtime)
makedepends=(dotnet-sdk)
checkdepends=()
optdepends=()
provides=(railworks-forge)
conflicts=(railworks-forge)
replaces=()
backup=()
options=()
install=

build() {
	cd "$srcdir/$pkgname-$pkgver"
	dotnet build --configuration Release
}

package() {
	cd "$srcdir/$pkgname-$pkgver"
	cp -v "RailworksForge/bin/Release/net8.0/linux-x64/publish/*" "$pkgdir/opt/$pkgname"
}
