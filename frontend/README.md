# AI Chat Assistant

A modern chat application built with Next.js, TypeScript, and Supabase that provides an interactive AI chat experience with templating capabilities.

## Architecture Overview

### Core Features
- Real-time AI chat interactions using OpenAI's GPT-3.5
- Streaming responses for a more dynamic experience
- Chat history persistence
- Template system for predefined chat scenarios
- Markdown rendering with syntax highlighting
- Dark mode support
- Responsive design

### Tech Stack
- **Frontend**: Next.js 14 with App Router
- **Language**: TypeScript
- **Database**: Supabase (PostgreSQL)
- **AI**: OpenAI GPT-3.5
- **Styling**: TailwindCSS
- **Markdown**: Marked + Prism.js

## Component Structure

### Pages
- `app/page.tsx`: Home page with question input and templates
- `app/chat/page.tsx`: Main chat interface with streaming responses
- `app/api/chat/route.ts`: API route for OpenAI interactions

### Components
- `QuestionInput.tsx`: Initial question input on home page
- `ChatTemplates.tsx`: Display and management of chat templates
- `ChatHistory.tsx`: Shows recent chat history

### Utilities
- `markdown.ts`: Markdown configuration and syntax highlighting
- `supabase.ts`: Supabase client and type definitions

## Database Schema

### Tables

#### Chats
```sql
create table chats (
  id uuid default uuid_generate_v4() primary key,
  title text not null,
  created_at timestamp with time zone default timezone('utc'::text, now()) not null,
  is_template boolean default false,
  description text,
  template_category text,
  template_image text
);
```

#### Messages
```sql
create table messages (
  id uuid default uuid_generate_v4() primary key,
  chat_id uuid references chats(id) on delete cascade not null,
  role text not null check (role in ('user', 'assistant')),
  content text not null,
  created_at timestamp with time zone default timezone('utc'::text, now()) not null
);
```

### Indexes
```sql
create index idx_templates on chats (is_template) where is_template = true;
```

## Environment Variables

Create a `.env.local` file with:

```env
# OpenAI API Key
OPENAI_API_KEY=your_openai_api_key

# Supabase Configuration
NEXT_PUBLIC_SUPABASE_URL=your_supabase_url
NEXT_PUBLIC_SUPABASE_ANON_KEY=your_supabase_anon_key
```

## Template Management

Templates can be managed via CLI:

```bash
# Install dependencies
npm install

# Create a new template
npm run templates create

# List all templates
npm run templates list

# Delete a template
npm run templates delete
```

## Features

### Chat System
- Real-time message streaming
- Markdown rendering with syntax highlighting
- Message history persistence
- Local storage for recent chats
- Dark mode support
- Responsive design

### Template System
- Convert existing chats to templates
- Categorize templates
- Custom icons/emojis for templates
- Template management via CLI
- Template preview and quick start

### Data Persistence
- Chat history stored in Supabase
- Message history with timestamps
- Template configuration
- Local storage for recent chats

## Development

1. Clone the repository
2. Install dependencies:
   ```bash
   npm install
   ```
3. Set up environment variables in `.env.local`
4. Run the development server:
   ```bash
   npm run dev
   ```

## Database Setup

1. Create a Supabase project
2. Run the SQL commands in the database schema section
3. Set up the environment variables
4. Enable Row Level Security (RLS) policies as needed

## Template CLI Usage

The template management CLI provides an interactive interface to:
- Convert existing chats to templates
- Add descriptions and categories
- Set custom images or emojis
- List all templates
- Delete templates

## Contributing

1. Fork the repository
2. Create a feature branch (e.g. `<username>/<feature-name>`)
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

MIT License

# TODOS
- [ ] add codeblock visualizer, and code showing region (will need to be able to classify whether code is visualizable or not - can tell if something is code by just whether if has backticks or not)
- [ ] add XR specific templates
- [ ] UI/UX improvements, logo, backgrounds/images (video background?)