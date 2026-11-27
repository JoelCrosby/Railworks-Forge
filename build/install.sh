#!/usr/bin/env bash
# Builds and installs the package from a scratch copy of the PKGBUILD, then deletes it. makepkg leaves the git clone,
# src/ and pkg/ trees and the built package behind and rewrites pkgver in the PKGBUILD; doing all of that in a
# throwaway directory keeps build/ clean whether the build succeeds or fails. Extra arguments go to makepkg.
set -euo pipefail

cd "$(dirname "$0")"

work=$(mktemp -d "$PWD/.makepkg.XXXXXX")
trap 'rm -rf "$work"' EXIT

cp PKGBUILD "$work/"
cd "$work"
makepkg -si "$@"
