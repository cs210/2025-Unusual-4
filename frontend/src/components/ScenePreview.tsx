"use client"

import type React from "react"

import { useEffect, useRef } from "react"
import * as THREE from "three"

interface ScenePreviewProps {
  code: string
}

const ScenePreview: React.FC<ScenePreviewProps> = ({ code }) => {
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!containerRef.current) return

    const scene = new THREE.Scene()
    const camera = new THREE.PerspectiveCamera(75, 1, 0.1, 1000)
    const renderer = new THREE.WebGLRenderer({ antialias: true })

    renderer.setSize(200, 200)
    containerRef.current.appendChild(renderer.domElement)

    // Add ambient light
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.5)
    scene.add(ambientLight)

    // Add directional light
    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.5)
    directionalLight.position.set(5, 5, 5)
    scene.add(directionalLight)

    // Set camera position
    camera.position.z = 5

    // Execute the provided code
    try {
      const executeCode = new Function(
        "THREE",
        "scene",
        "camera",
        `
        try {
          ${code}
        } catch (error) {
          console.error("Error executing preview code:", error);
        }
      `,
      )

      executeCode(THREE, scene, camera)
    } catch (error) {
      console.error("Error setting up preview scene:", error)
    }

    // Render the scene
    const animate = () => {
      requestAnimationFrame(animate)
      renderer.render(scene, camera)
    }
    animate()

    // Clean up
    return () => {
      if (containerRef.current) {
        containerRef.current.removeChild(renderer.domElement)
      }
    }
  }, [code])

  return <div ref={containerRef} className="w-[200px] h-[200px] rounded-lg overflow-hidden" />
}

export default ScenePreview

