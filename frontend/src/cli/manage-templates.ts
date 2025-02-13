#!/usr/bin/env node

import { createClient } from '@supabase/supabase-js'
import { config } from 'dotenv'
import { Command } from 'commander'
import prompts from 'prompts'
import chalk from 'chalk'

// Load environment variables from .env.local
config({ path: '.env.local' })

const supabase = createClient(
  process.env.NEXT_PUBLIC_SUPABASE_URL!,
  process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY!
)

const program = new Command()

program
  .name('chat-templates')
  .description('CLI to manage chat templates')
  .version('1.0.0')

program
  .command('create')
  .description('Create a new template from an existing chat')
  .action(async () => {
    try {
      // Get list of non-template chats
      const { data: chats } = await supabase
        .from('chats')
        .select('*')
        .eq('is_template', false)
        .order('created_at', { ascending: false })

      if (!chats?.length) {
        console.log(chalk.yellow('No chats found to convert to templates'))
        return
      }

      const response = await prompts([
        {
          type: 'select',
          name: 'chatId',
          message: 'Select a chat to convert to template',
          choices: chats.map(chat => ({
            title: `${chat.title} (${new Date(chat.created_at).toLocaleDateString()})`,
            value: chat.id
          }))
        },
        {
          type: 'text',
          name: 'title',
          message: 'Enter template title',
          validate: value => value.length > 0
        },
        {
          type: 'text',
          name: 'description',
          message: 'Enter template description',
          validate: value => value.length > 0
        },
        {
          type: 'text',
          name: 'category',
          message: 'Enter template category (e.g., Writing, Coding, Math)',
          validate: value => value.length > 0
        },
        {
          type: 'text',
          name: 'image',
          message: 'Enter image URL (or emoji for default icon)',
          initial: '🤖'
        }
      ])

      // Update the chat to be a template
      const { error } = await supabase
        .from('chats')
        .update({
          is_template: true,
          title: response.title,
          description: response.description,
          template_category: response.category,
          template_image: response.image
        })
        .eq('id', response.chatId)

      if (error) throw error

      console.log(chalk.green('✓ Template created successfully!'))
    } catch (error) {
      console.error(chalk.red('Error creating template:'), error)
    }
  })

program
  .command('list')
  .description('List all templates')
  .action(async () => {
    try {
      const { data: templates } = await supabase
        .from('chats')
        .select('*')
        .eq('is_template', true)
        .order('template_category', { ascending: true })

      if (!templates?.length) {
        console.log(chalk.yellow('No templates found'))
        return
      }

      console.log(chalk.blue('\nCurrent Templates:'))
      templates.forEach(template => {
        console.log(chalk.green(`\n${template.title}`))
        console.log(chalk.gray(`Category: ${template.template_category}`))
        console.log(chalk.gray(`Description: ${template.description}`))
        console.log(chalk.gray(`Icon: ${template.template_image}`))
      })
    } catch (error) {
      console.error(chalk.red('Error listing templates:'), error)
    }
  })

program
  .command('delete')
  .description('Delete a template')
  .action(async () => {
    try {
      const { data: templates } = await supabase
        .from('chats')
        .select('*')
        .eq('is_template', true)
        .order('title', { ascending: true })

      if (!templates?.length) {
        console.log(chalk.yellow('No templates found'))
        return
      }

      const response = await prompts({
        type: 'select',
        name: 'templateId',
        message: 'Select a template to delete',
        choices: templates.map(template => ({
          title: template.title,
          value: template.id
        }))
      })

      const { error } = await supabase
        .from('chats')
        .delete()
        .eq('id', response.templateId)

      if (error) throw error

      console.log(chalk.green('✓ Template deleted successfully!'))
    } catch (error) {
      console.error(chalk.red('Error deleting template:'), error)
    }
  })

program.parse() 