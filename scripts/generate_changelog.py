#!/usr/bin/env python3
import argparse
import subprocess
import re
import sys

def get_norm_key(msg):
    # Lower, strip leading type+scope, remove punctuation & articles
    n = msg.lower()
    n = re.sub(r'^(feat|fix|docs|style|refactor|perf|test|chore|ci|security|build|api|db|ui|ux|revert)(\([^)]*\))?(!)?:\s*', '', n)
    n = re.sub(r'[\.\!\?\,\;\:\"\'`]', '', n)
    n = re.sub(r'\b(the|a|an|for|of|in|to|with|from|on|at|by)\b', '', n)
    n = re.sub(r'\s+', ' ', n)
    return n.strip()

def get_formatted_item(item, repo):
    if item['hashes']:
        links = []
        for h in item['hashes']:
            if repo:
                links.append(f"[{h}](https://github.com/{repo}/commit/{h})")
            else:
                links.append(f"`{h}`")
        hash_str = ", ".join(links)
        return f"{item['display']} ({hash_str})"
    return item['display']

def main():
    parser = argparse.ArgumentParser(description="Generates a structured, deduplicated, user-friendly changelog from git commit history.")
    parser.add_argument("--from-tag", default="", help="Git ref to start log from.")
    parser.add_argument("--total-commits", default="", help="Raw commit count to show in footer.")
    parser.add_argument("--repo", default="", help="GitHub repository name (owner/repo).")
    args = parser.parse_args()

    # Noise filter patterns
    noise_patterns = [
        r'^\s*$',
        # Lazy file-only commits
        r'^(Update|Aktualisier[et]?|Add|Adds|Adde|Delete|Deletes|Remove|Removes|Rename|Renames|Move|Moves|Fix|Edit|Change|Modify)\s+[\w\-\.\/]+\.\w{1,10}\s*$',
        # Merge commits
        r'^Merge (pull request|branch|remote-tracking branch)\b',
        r'^Merge from\b',
        # Version bumps committed by bots
        r'^(chore|build)(\([^)]*\))?:\s*(bump|release|version)\b',
        r'^(bump|release)(\s+version)?\s+v?\d',
        r'^v?\d+\.\d+\.\d+\s*$',
        # CI skip markers
        r'^\[skip[- ]ci\]',
        r'^chore: regenerate (manifest|connections|changelog)\b',
        r'^chore: update (project_manifest|project_connections)\b',
        # Bot / automated commits
        r'^(auto.?generated?|automated?|bot:)\b',
        r'^Revert "Revert',
        r'^Initial commit\s*$',
        r'^WIP\b',
        r'^wip\b',
        r'^.{1,3}$',
        r'\[skip[- ]ci\]\s*$'
    ]

    category_order = ['breaking', 'feat', 'fix', 'security', 'perf', 'refactor', 'api', 'db', 'ui', 'docs', 'test', 'ci', 'chore', 'other']
    category_emoji = {
        'breaking': '💥 Breaking Changes',
        'feat': '✨ New Features',
        'fix': '🐛 Bug Fixes',
        'security': '🔒 Security',
        'perf': '⚡ Performance',
        'refactor': '♻️ Code Improvements',
        'api': '🔌 API Changes',
        'db': '🗄️ Database',
        'ui': '🎨 UI / UX',
        'docs': '📚 Documentation',
        'test': '🧪 Tests',
        'ci': '🔄 CI / CD',
        'chore': '🔧 Maintenance',
        'other': '📦 Other Changes'
    }

    type_map = {
        'feat': 'feat', 'feature': 'feat',
        'fix': 'fix', 'bugfix': 'fix', 'hotfix': 'fix',
        'security': 'security', 'sec': 'security',
        'perf': 'perf', 'optim': 'perf',
        'refactor': 'refactor', 'refact': 'refactor',
        'api': 'api',
        'db': 'db', 'migration': 'db', 'migrate': 'db', 'schema': 'db',
        'ui': 'ui', 'style': 'ui', 'ux': 'ui',
        'docs': 'docs', 'doc': 'docs',
        'test': 'test', 'tests': 'test',
        'ci': 'ci', 'cd': 'ci', 'build': 'ci',
        'chore': 'chore', 'maint': 'chore', 'infra': 'chore', 'deps': 'chore', 'dep': 'chore', 'bump': 'chore',
        'revert': 'fix'
    }

    scope_map = {
        'api': 'api', 'endpoint': 'api', 'router': 'api', 'route': 'api',
        'db': 'db', 'database': 'db', 'migration': 'db', 'schema': 'db', 'model': 'db',
        'ui': 'ui', 'frontend': 'ui', 'fe': 'ui', 'component': 'ui', 'modal': 'ui', 'dashboard': 'ui',
        'security': 'security', 'auth': 'security', 'authz': 'security', 'authn': 'security', 'jwt': 'security', 'rbac': 'security',
        'ci': 'ci', 'cd': 'ci', 'workflow': 'ci', 'docker': 'ci', 'dockerfile': 'ci', 'actions': 'ci'
    }

    max_per_section = 15
    never_collapse = {'breaking', 'security'}

    # Run git log
    git_args = ["git", "log", "--pretty=format:%h %s"]
    if args.from_tag:
        git_args.append(f"{args.from_tag}..HEAD")
    else:
        git_args.extend(["--max-count=2000"])

    try:
        raw_output = subprocess.check_output(git_args, stderr=subprocess.DEVNULL).decode('utf-8', errors='ignore')
        commit_lines = [line.strip() for line in raw_output.splitlines() if line.strip()]
    except Exception:
        commit_lines = []

    total_raw = int(args.total_commits) if args.total_commits else len(commit_lines)

    buckets = {k: [] for k in category_order}
    seen_items = {}

    for line in commit_lines:
        match = re.match(r'^([0-9a-fA-F]+)\s+(.*)$', line)
        if match:
            commit_hash = match.group(1)
            msg = match.group(2).strip()
        else:
            commit_hash = ""
            msg = line.strip()

        if not msg:
            continue

        # Noise check
        skip = False
        for pattern in noise_patterns:
            if re.search(pattern, msg):
                skip = True
                break
        if skip:
            continue

        # Parse conventional commit
        bucket = 'other'
        display = msg
        is_break = False

        # Pattern: type[(scope)][!]: description
        conv_match = re.match(r'^([A-Za-z][A-Za-z0-9_-]*)(\([^)]*\))?(!)?:\s*(.+)$', msg)
        if conv_match:
            raw_type = conv_match.group(1).lower()
            raw_scope = conv_match.group(2).replace('(', '').replace(')', '').lower().strip() if conv_match.group(2) else ''
            is_break = conv_match.group(3) == '!'
            desc = conv_match.group(4).strip()

            if raw_scope and raw_scope in scope_map:
                bucket = scope_map[raw_scope]
            elif raw_type in type_map:
                bucket = type_map[raw_type]

            desc_cap = desc[0].upper() + desc[1:] if desc else desc
            display = f"**{raw_scope}:** {desc_cap}" if raw_scope else desc_cap
        else:
            # Fallback freeform parsing
            display = msg[0].upper() + msg[1:] if msg else msg
            msg_lower = msg.lower()
            if re.search(r'\b(general\s+fix|small\s+fix|bug\s+fix|fix(es|ed)?\b|fix\s+\w|general\s+improve|improvements?\s+reported)', msg_lower):
                bucket = 'fix'
            elif re.search(r'\b(ci\b|linter?|lint\s+fix|pipeline|workflow|github\s+action|generate[_\s]changelog|changelog\s+(categor|generat|script|fix)|test\s+crawler|ui\s+test\s+crawler|crawler\s+wait|docker(file)?|container)\b', msg_lower):
                bucket = 'ci'
            elif re.search(r'\b(update\s+depend|bump\s+depend|renovate|dependency\s+update|upgrade\s+dep)', msg_lower):
                bucket = 'chore'
            elif re.search(r'\b(add\s+missing\s+ui|improved?\s+mail|improved?\s+cusm|improved?\s+ad\s+group|add(ed|s)?\s+(missing\s+)?(ui|feature|support|ability)|new\s+feature)', msg_lower):
                bucket = 'feat'
            elif re.search(r'\b(security|vulnerability|cve|auth(en|oriz))', msg_lower):
                bucket = 'security'
            elif re.search(r'\b(perf(ormance)?|speed|faster|slower|optim|latency|throughput)', msg_lower):
                bucket = 'perf'
            elif re.search(r'\b(refactor(ing)?|restructur(e|ing)|rewrite|clean.?up|improve(d|s|ment|ing)?s?)\b', msg_lower):
                bucket = 'refactor'
            elif re.search(r'\b(doc(s|ument(ation)?)?|readme|wiki|guide)\b', msg_lower):
                bucket = 'docs'
            elif re.search(r'\b(test(s|ing)?|spec|unit\s+test|e2e)', msg_lower):
                bucket = 'test'
            elif 'changelog' in msg_lower:
                bucket = 'ci'
            elif re.search(r'\b(ui\b|ux\b|frontend|layout|style|theme|design|modal|component|dashboard|template|outlook|mail\s+template)', msg_lower):
                bucket = 'ui'
            elif re.search(r'\b(api\b|endpoint|route|router|swagger|openapi)', msg_lower):
                bucket = 'api'
            elif re.search(r'\b(db\b|database|migration|schema|model|sql)', msg_lower):
                bucket = 'db'
            elif re.search(r'\b(chore|maint(enance)?|housekeep|dependen|package|infra)', msg_lower):
                bucket = 'chore'

        norm_key = get_norm_key(display)

        if is_break:
            break_display = f"**{display}**"
            break_key = f"breaking:{norm_key}"
            if break_key in seen_items:
                if commit_hash and commit_hash not in seen_items[break_key]['hashes']:
                    seen_items[break_key]['hashes'].append(commit_hash)
            else:
                break_item = {'display': break_display, 'hashes': [commit_hash] if commit_hash else []}
                seen_items[break_key] = break_item
                buckets['breaking'].append(break_item)

        if norm_key in seen_items:
            if commit_hash and commit_hash not in seen_items[norm_key]['hashes']:
                seen_items[norm_key]['hashes'].append(commit_hash)
            continue

        item = {'display': display, 'hashes': [commit_hash] if commit_hash else []}
        seen_items[norm_key] = item
        buckets[bucket].append(item)

    # Build output
    out = []
    has_any = False
    filtered_count = sum(len(buckets[k]) for k in category_order)

    if buckets['breaking']:
        has_any = True
        out.append('> [!CAUTION]')
        out.append('> **This release contains breaking changes. Please review before updating.**')
        out.append('>')
        for item in buckets['breaking']:
            formatted = get_formatted_item(item, args.repo)
            out.append(f"> - {formatted}")
        out.append('')

    for key in category_order:
        if key == 'breaking':
            continue
        bucket = buckets[key]
        if not bucket:
            continue
        has_any = True

        out.append(f"### {category_emoji[key]}")
        out.append('')

        collapse = (len(bucket) > max_per_section) and (key not in never_collapse)

        if collapse:
            for i in range(max_per_section):
                formatted = get_formatted_item(bucket[i], args.repo)
                out.append(f"- {formatted}")
            remaining = len(bucket) - max_per_section
            out.append('')
            out.append("<details>")
            out.append(f"<summary>Show {remaining} more changes…</summary>")
            out.append('')
            for i in range(max_per_section, len(bucket)):
                formatted = get_formatted_item(bucket[i], args.repo)
                out.append(f"- {formatted}")
            out.append('')
            out.append("</details>")
        else:
            for item in bucket:
                formatted = get_formatted_item(item, args.repo)
                out.append(f"- {formatted}")
        out.append('')

    if not has_any:
        out.append('> *No categorised changes found in this release.*')
        out.append('> Most commits were maintenance, dependency updates, or automated changes.')
        out.append('')

    out.append('---')

    if total_raw > 0:
        out.append(f"*{filtered_count} significant changes from {total_raw} total commits since `{args.from_tag or 'initial'}`.*")
    else:
        out.append(f"*Changelog generated.*")

    print("\n".join(out))

if __name__ == "__main__":
    main()
