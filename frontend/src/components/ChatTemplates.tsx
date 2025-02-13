'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { supabase } from '@/utils/supabase'
import type { Chat } from '@/utils/supabase'

const ChatTemplates = () => {
  const router = useRouter()
  const [templates, setTemplates] = useState<Chat[]>([])

  useEffect(() => {
    const loadTemplates = async () => {
      const { data } = await supabase
        .from('chats')
        .select('*')
        .eq('is_template', true)
        .order('template_category', { ascending: true })

      if (data) {
        setTemplates(data)
      }
    }

    loadTemplates()
  }, [])

  const handleTemplateClick = (template: Chat) => {
    router.push(`/chat?template=${template.id}`)
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      {templates.map((template) => (
        <button
          key={template.id}
          onClick={() => handleTemplateClick(template)}
          className="p-6 rounded-lg border border-gray-200 dark:border-gray-700 hover:border-blue-500 dark:hover:border-blue-400 transition-colors duration-200 text-left bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
          tabIndex={0}
          aria-label={`Start ${template.title} chat`}
        >
          <div className="flex items-center gap-3 mb-4">
            <span className="text-2xl">{template.template_image}</span>
            <div>
              <h3 className="font-medium">{template.title}</h3>
              <p className="text-sm text-gray-500 dark:text-gray-400">
                {template.template_category}
              </p>
            </div>
          </div>
          <p className="text-sm text-gray-600 dark:text-gray-300">
            {template.description}
          </p>
        </button>
      ))}
    </div>
  )
}

export default ChatTemplates 