import OpenAI from 'openai'
import { NextResponse } from 'next/server'
import { type NextRequest } from 'next/server'

// Define message type
interface ChatMessage {
  role: 'user' | 'assistant' | 'system'
  content: string
}

// Define request body type
interface RequestBody {
  message: string
  messages: ChatMessage[]
}

// Initialize OpenAI client
const openai = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY
})

export async function POST(req: NextRequest) {
  try {
    const { message, messages } = (await req.json()) as RequestBody
    
    // Define the system instruction that tells GPT-3.5-turbo not to recreate the Three.js setup
    const systemInstruction: ChatMessage = {
      role: 'system',
      content: `
You are a code assistant that generates Three.js code.
Assume that a Three.js scene, camera, renderer, and animation loop are already created in the SceneView component.
The camera starts at [0, 0, 0]. If you create any large objects (like a sphere with radius > 1), please move the camera backward (for example, set camera.position.z = 15) so that the object is visible.
Do not generate code that creates a new scene, camera, or renderer, or that attaches the renderer to the DOM or starts a new animation loop.

Instead, generate code that only creates or updates objects (such as meshes) and adds them to the existing scene.
When creating objects, assign each a unique name (for example, object.name = 'uniqueName') and check for existing objects using scene.getObjectByName('uniqueName') or a provided global registry.
If the object exists, update its properties (for example, change its material color, position, or scale) rather than creating a duplicate.
A helper function called updateOrCreate(name, createFn, updateFn) is available in the context; always use this function to encapsulate the logic for creating or updating any object.
Ensure your generated code works generically for any object type the user wants to create, not just spheres.
    `
    }

    // Create stream with the system instruction prepended
    const stream = await openai.chat.completions.create({
      model: 'gpt-3.5-turbo',
      messages: [
        systemInstruction,
        ...messages.map((msg: ChatMessage) => ({
          role: msg.role,
          content: msg.content
        })),
        { role: 'user', content: message }
      ],
      stream: true
    })

    // Create a new ReadableStream to stream the response back to the client
    const readableStream = new ReadableStream({
      async start(controller) {
        for await (const chunk of stream) {
          const text = chunk.choices[0]?.delta?.content || ''
          controller.enqueue(text)
        }
        controller.close()
      }
    })

    return new Response(readableStream, {
      headers: {
        'Content-Type': 'text/event-stream',
        'Cache-Control': 'no-cache',
        'Connection': 'keep-alive',
      },
    })
  } catch (error) {
    console.error('Error:', error)
    return NextResponse.json(
      { error: 'An error occurred during your request.' },
      { status: 500 }
    )
  }
}
