# SonarCloud Scenario

This folder contains the SonarCloud scenario artifacts (screenshots and notes) used to demonstrate SonarCloud analysis and integration for this repository.

Overview
- Purpose: Show expected SonarCloud results and provide reproduction notes for the sample project and CI workflow.
- Contents: screenshots of the analysis results and short instructions.

Files
- SonarCloud screenshots: images showing success/failure states and key UI panels.

How to reproduce locally
1. Install SonarScanner CLI: follow https://sonarcloud.io/documentation
2. Authenticate using a SonarCloud token and the project key
3. Run scanner from the project root:
```
sonar-scanner \
  -Dsonar.projectKey=your_project_key \
  -Dsonar.organization=your_org \
  -Dsonar.login=YOUR_TOKEN
```

GitHub Actions
- This repository may include a GitHub Actions workflow that runs SonarCloud analysis on push/PR. Check `.github/workflows/` for a SonarCloud workflow file.

Notes
- Replace `your_project_key`, `your_org`, and `YOUR_TOKEN` with real values.
- For full setup instructions see SonarCloud documentation: https://sonarcloud.io/documentation
