# CodeQL Scenario

This folder contains the CodeQL scenario artifacts (screenshots and notes) used to demonstrate GitHub CodeQL analysis and security scanning for this repository.

## Overview

- **Purpose**: Show expected CodeQL results, security vulnerability detection, and provide reproduction notes for the sample project and CI workflow.
- **Contents**: Screenshots of the analysis results, Security tab insights, and step-by-step instructions.
- **Scope**: Demonstrates code quality analysis, security scanning, and vulnerability detection using GitHub's native CodeQL capabilities.

## Files

- **CodeQL screenshots**: Images showing successful scans, vulnerability detection, security alerts, and key UI panels from the Security tab.

## How to Enable CodeQL in GitHub Advanced Settings

### Step 1: Navigate to Repository Settings

1. Go to your GitHub repository
2. Click on **Settings** (gear icon) in the top right
3. In the left sidebar, click on **Code security and analysis**

### Step 2: Enable Dependency Graph (Optional but Recommended)

1. Look for **Dependency graph** section
2. Click **Enable** (if not already enabled)
3. This helps identify dependencies and vulnerabilities

### Step 3: Enable Secret Scanning

1. In the **Secret scanning** section, click **Enable**
2. This automatically scans for exposed secrets and credentials
3. You'll receive alerts for any detected secrets

### Step 4: Enable CodeQL Analysis

1. Scroll to the **Code scanning** section
2. Click **Set up** or **Enable** next to CodeQL analysis
3. You have two options:
   - **Default**: Uses GitHub's pre-configured CodeQL workflow
   - **Advanced**: Customize the workflow for your needs (recommended for complex projects)

4. If you select **Advanced**:
   - A new file `codeql.yml` will be created in `.github/workflows/`
   - Review and customize the workflow if needed
   - Click **Create** to confirm

### Step 5: Configure Workflow (if using Advanced)

The typical CodeQL workflow includes:

```yaml
name: CodeQL Analysis

on:
  push:
    branches: [ "main", "develop" ]
  pull_request:
    branches: [ "main" ]
  schedule:
    - cron: '0 0 * * 0'  # Weekly scan on Sunday at midnight

permissions:
  contents: read
  security-events: write

jobs:
  analyze:
    name: Analyze
    runs-on: ubuntu-latest

    strategy:
      fail-fast: false
      matrix:
        language: [ 'javascript', 'python' ]  # Add languages used in your repo

    steps:
    - name: Checkout repository
      uses: actions/checkout@v4

    - name: Initialize CodeQL
      uses: github/codeql-action/init@v3
      with:
        languages: ${{ matrix.language }}

    - name: Autobuild
      uses: github/codeql-action/autobuild@v3

    - name: Perform CodeQL Analysis
      uses: github/codeql-action/analyze@v3
      with:
        category: "/language:${{ matrix.language }}"
```

### Step 6: Verify Settings

1. Once enabled, CodeQL will automatically run on:
   - Every push to default branches
   - Every pull request
   - On a scheduled basis (typically weekly)

2. Go to the **Security** tab to view:
   - **Code scanning alerts**: Security issues detected by CodeQL
   - **Security advisories**: Vulnerable dependencies
   - **Secret scanning alerts**: Exposed secrets

## How to Reproduce Locally

### Prerequisites

1. **Download CodeQL CLI**: Visit https://github.com/github/codeql-cli-binaries/releases
2. **Install CodeQL**: 
   ```bash
   # Extract the archive
   unzip codeql-linux64.zip
   
   # Add to PATH
   export PATH=$PATH:/path/to/codeql
   ```

3. **Download CodeQL Database**:
   ```bash
   # For JavaScript/TypeScript
   codeql database create <database-dir> --language=javascript --source-root=.
   ```

### Run Local Analysis

```bash
# Create a CodeQL database
codeql database create my-codeql-db --language=javascript --source-root=.

# Run analysis with default suite
codeql database analyze my-codeql-db javascript-code-scanning.qls --format=sarif-latest --output=results.sarif

# Or run with custom queries
codeql database analyze my-codeql-db javascript-security-and-quality.qls --format=sarif-latest --output=results.sarif
```

### View Results

```bash
# Convert SARIF to human-readable format (if needed)
# Or import results.sarif directly into GitHub via the REST API
```

## GitHub Actions Integration

### Default CodeQL Workflow

When you enable CodeQL in **Code security and analysis** settings:

1. GitHub automatically creates a workflow (or uses existing one)
2. Workflow triggers on:
   - Push events
   - Pull request events
   - Scheduled daily/weekly runs
3. Results appear in the **Security** tab under **Code scanning**

### Workflow Triggers

- **On Push**: Analyzes code when you push to default branches
- **On Pull Request**: Checks code changes in PRs before merge
- **On Schedule**: Runs periodic scans (e.g., every Sunday at midnight)

### Viewing Results

1. Go to your repository's **Security** tab
2. Click **Code scanning alerts** in the left menu
3. View detected vulnerabilities with:
   - Issue type
   - Severity level (Critical, High, Medium, Low, Note)
   - Affected code location
   - Recommended fixes

## Supported Languages

CodeQL supports analysis for:

- **JavaScript/TypeScript**
- **Python**
- **Java**
- **C/C++**
- **C#**
- **Go**
- **Ruby**
- **Swift**

## Troubleshooting

### Issue: CodeQL Analysis Fails

**Solution**:
1. Check workflow logs in the **Actions** tab
2. Ensure your repository has:
   - At least some source code files
   - Proper read permissions for the workflow
3. Verify that the specified language is correct

### Issue: No Results Appear

**Solution**:
1. Wait a few minutes for the workflow to complete
2. Check the **Actions** tab to confirm the workflow ran successfully
3. Go to **Security** tab → **Code scanning** to view results
4. If still empty, your code may have no detected issues (which is good!)

### Issue: Permission Denied

**Solution**:
1. Go to **Settings** → **Actions** → **General**
2. Ensure **Workflow permissions** is set to **Read and write**
3. Check that your repository allows Actions to write security events

### Issue: Language Not Detected

**Solution**:
1. Specify languages explicitly in the workflow configuration
2. Ensure source files have proper extensions (.js, .py, .java, etc.)
3. Check that files are not in `.gitignore`

## Notes and Best Practices

- **Regular Scanning**: CodeQL runs on every push and PR by default—no manual intervention needed
- **Default Queries**: GitHub provides curated queries for security and quality checks
- **Custom Queries**: Advanced users can write custom CodeQL queries for specific patterns
- **Integration with Branch Protection**: Use CodeQL results in branch protection rules to require fixes before merge
- **Cost**: CodeQL analysis is included free for public repositories and GitHub Advanced Security licensees
- **Performance**: For large repositories, CodeQL analysis may take several minutes

## Related Documentation

- [GitHub CodeQL Documentation](https://codeql.github.com/docs/)
- [Code Scanning with CodeQL](https://docs.github.com/en/code-security/code-scanning/introduction-to-code-scanning/about-code-scanning)
- [CodeQL CLI Documentation](https://codeql.github.com/docs/codeql-cli/)
- [Security Best Practices](https://docs.github.com/en/code-security)

## For More Information

Refer to the official GitHub documentation on code security at:
https://docs.github.com/en/code-security
