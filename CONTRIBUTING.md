# Contributing to Send to Kindle

Thank you for considering contributing to Send to Kindle! This document provides guidelines for contributing to the project.

## Getting Started

1. Fork the repository
2. Clone your fork locally
3. Create a new branch for your feature or bugfix
4. Make your changes
5. Test your changes thoroughly
6. Commit with a clear message
7. Push to your fork
8. Submit a pull request

## Development Setup

Follow the setup instructions in the main [README.md](README.md) to get your development environment ready.

## Code Style

### TypeScript/JavaScript

- Use TypeScript for type safety
- Follow the existing code style
- Use meaningful variable and function names
- Add comments for complex logic
- Use async/await instead of promises

### C#

- Follow standard C# naming conventions
- Use dependency injection
- Add XML documentation comments for public APIs
- Handle exceptions appropriately
- Use async/await for I/O operations

### Astro

- Use TypeScript for component scripts
- Follow the Astro file structure conventions
- Keep components small and focused
- Use props for component configuration

## Testing

- Test your changes locally before submitting
- For the extension: Test in both Chrome and Edge
- For the web app: Test both authenticated and unauthenticated states
- For Azure Functions: Test both local and production modes

## Commit Messages

Use clear and descriptive commit messages:

- `feat: Add new feature`
- `fix: Fix bug in EPUB conversion`
- `docs: Update README with new instructions`
- `refactor: Improve code structure`
- `test: Add tests for email service`

## Pull Request Process

1. Update the README.md with details of changes if needed
2. Ensure your code follows the existing style
3. Test your changes thoroughly
4. Update documentation if you're changing functionality
5. Your PR will be reviewed by maintainers

## Reporting Bugs

When reporting bugs, please include:

- Clear description of the issue
- Steps to reproduce
- Expected behavior
- Actual behavior
- Environment details (OS, browser, Node version, etc.)
- Screenshots if applicable

## Feature Requests

We welcome feature requests! Please:

- Check if the feature has already been requested
- Provide a clear description of the feature
- Explain why it would be useful
- Consider how it fits with existing features

## Code of Conduct

- Be respectful and inclusive
- Welcome newcomers
- Provide constructive feedback
- Focus on what is best for the community

## Questions?

Feel free to open an issue with your question, or reach out to the maintainers.

Thank you for contributing! 🎉
