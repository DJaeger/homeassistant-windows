#!/usr/bin/env python3
import os
import re
import subprocess
import sys

def main():
    release_type = os.environ.get("RELEASE_TYPE", "dev")
    increment_level = os.environ.get("INCREMENT_LEVEL", "patch")
    version_override = os.environ.get("VERSION_OVERRIDE", "")
    re_release = os.environ.get("RE_RELEASE", "false")
    repo = os.environ.get("REPO", "").lower()
    github_token = os.environ.get("GITHUB_TOKEN", "")

    # Get latest tag
    latest_tag = ""
    if github_token and repo:
        try:
            cmd = ["gh", "api", f"repos/{repo}/tags", "--jq", ".[].name"]
            output = subprocess.check_output(cmd, stderr=subprocess.DEVNULL).decode("utf-8")
            tags = [t.strip() for t in output.splitlines() if re.match(r'^v?\d+\.\d+\.\d+(?:(?:b|-dev)\d+)?$', t.strip())]
            if tags:
                latest_tag = tags[0]
        except Exception:
            pass

    if not latest_tag:
        try:
            cmd = ["git", "tag", "-l", "[0-9]*", "v[0-9]*", "--sort=-v:refname"]
            output = subprocess.check_output(cmd, stderr=subprocess.DEVNULL).decode("utf-8")
            tags = [t.strip() for t in output.splitlines() if re.match(r'^v?\d+\.\d+\.\d+(?:(?:b|-dev)\d+)?$', t.strip())]
            if tags:
                latest_tag = tags[0]
        except Exception:
            pass

    print(f"Latest tag detected: {latest_tag}")

    # Read VERSION file
    current_version = "2026.3.0"
    if os.path.exists("VERSION"):
        with open("VERSION", "r", encoding="utf-8") as f:
            current_version = f.read().strip()

    if re_release == "true":
        version = current_version
        tag = f"v{version}"
        is_prerelease = "true" if ("b" in version or "-dev" in version) else "false"
        base_version = re.match(r'^(\d+\.\d+\.\d+)', version).group(1) if re.match(r'^(\d+\.\d+\.\d+)', version) else version
    else:
        # Get calendar year and month
        import datetime
        now = datetime.datetime.now()
        current_year = now.year
        current_month = now.month

        # Determine latest parts
        latest_major, latest_minor, latest_patch = current_year, current_month, 0
        latest_pre_type = None
        latest_pre_num = None

        if latest_tag:
            tag_match = re.match(r'^v?(\d+)\.(\d+)\.(\d+)(?:(b|-dev)(\d+))?', latest_tag)
            if tag_match:
                latest_major = int(tag_match.group(1))
                latest_minor = int(tag_match.group(2))
                latest_patch = int(tag_match.group(3))
                latest_pre_type = tag_match.group(4)
                latest_pre_num = int(tag_match.group(5)) if tag_match.group(5) is not None else None
        else:
            curr_match = re.match(r'^(\d+)\.(\d+)\.(\d+)', current_version)
            if curr_match:
                latest_major = int(curr_match.group(1))
                latest_minor = int(curr_match.group(2))
                latest_patch = int(curr_match.group(3))

        # CalVer: If calendar year/month is newer than latest tag, reset patch to 0
        if current_year > latest_major or (current_year == latest_major and current_month > latest_minor):
            base_major = current_year
            base_minor = current_month
            base_patch = 0
        else:
            base_major, base_minor, base_patch = latest_major, latest_minor, latest_patch

            if increment_level == "major":
                if not (latest_pre_type and latest_minor == 0 and latest_patch == 0):
                    base_major += 1
                    base_minor = 0
                    base_patch = 0
            elif increment_level == "minor":
                if not (latest_pre_type and latest_patch == 0):
                    base_minor += 1
                    base_patch = 0
            elif increment_level == "patch":
                if not latest_pre_type:
                    base_patch += 1

        base_version = f"{base_major}.{base_minor}.{base_patch}"
        if version_override:
            base_version = version_override

        if release_type == "stable":
            version = base_version
            tag = f"v{version}"
            is_prerelease = "false"
        else:
            is_prerelease = "true"
            prefix = "b" if release_type == "beta" else "-dev"

            if latest_pre_type == prefix and f"{latest_major}.{latest_minor}.{latest_patch}" == base_version:
                next_n = latest_pre_num + 1
            else:
                pattern = f"v{base_version}{prefix}*"
                try:
                    tags_out = subprocess.check_output(["git", "tag", "-l", pattern]).decode("utf-8")
                    tags = [t.strip() for t in tags_out.splitlines() if t.strip()]
                except Exception:
                    tags = []
                max_n = -1
                for t in tags:
                    n_match = re.search(rf"{re.escape(prefix)}(\d+)", t)
                    if n_match:
                        n = int(n_match.group(1))
                        if n > max_n:
                            max_n = n
                next_n = max_n + 1

            if release_type == "beta":
                version = f"{base_version}b{next_n}"
                tag = f"v{version}"
            else:
                try:
                    sha = subprocess.check_output(["git", "rev-parse", "--short", "HEAD"]).decode("utf-8").strip()
                except Exception:
                    sha = "unknown"
                version = f"{base_version}-dev{next_n}+{sha}"
                tag = f"v{version}"

    # Determine changelog starting tag
    changelog_from = ""
    skip_tag = tag if re_release == "true" else None

    try:
        all_tags_out = subprocess.check_output(["git", "tag", "-l", "--sort=-v:refname"]).decode("utf-8")
        all_tags = [t.strip() for t in all_tags_out.splitlines() if t.strip()]
    except Exception:
        all_tags = []

    if release_type == "stable":
        stable_tags = [t for t in all_tags if re.match(r'^v?\d+\.\d+\.\d+$', t) and t != skip_tag]
        if stable_tags:
            changelog_from = stable_tags[0]
    elif release_type == "beta":
        prev_betas = [t for t in all_tags if re.match(rf'^v?{re.escape(base_version)}b\d+$', t) and t != skip_tag]
        if prev_betas:
            changelog_from = prev_betas[0]
        else:
            stable_tags = [t for t in all_tags if re.match(r'^v?\d+\.\d+\.\d+$', t) and t != skip_tag]
            if stable_tags:
                changelog_from = stable_tags[0]
    else:
        dev_tags = [t for t in all_tags if re.match(r'^v?\d+\.\d+\.\d+', t) and t != skip_tag]
        if dev_tags:
            changelog_from = dev_tags[0]

    # Count commits
    total_commit_count = 0
    try:
        count_range = f"{changelog_from}..HEAD" if changelog_from else "HEAD"
        total_commit_count = int(subprocess.check_output(["git", "rev-list", "--count", count_range]).decode("utf-8").strip())
    except Exception:
        pass

    # Generate Changelog
    changelog_md = "_No changes detected._"
    if os.path.exists("scripts/generate_changelog.py"):
        try:
            cmd = ["python", "scripts/generate_changelog.py", "--from-tag", changelog_from, "--total-commits", str(total_commit_count), "--repo", repo]
            changelog_md = subprocess.check_output(cmd).decode("utf-8")
        except Exception as e:
            changelog_md = f"_Changelog generation failed: {e}_"

    body = f"## Release {version}\n\n{changelog_md}"
    with open("release_body.md", "w", encoding="utf-8") as f:
        f.write(body)

    # Set GitHub Actions output
    github_output = os.environ.get("GITHUB_OUTPUT")
    if github_output:
        with open(github_output, "a", encoding="utf-8") as f:
            f.write(f"version={version}\n")
            f.write(f"tag={tag}\n")
            f.write(f"is_prerelease={is_prerelease}\n")

if __name__ == "__main__":
    main()
