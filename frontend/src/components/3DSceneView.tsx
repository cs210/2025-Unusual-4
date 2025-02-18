"use client"

import type React from "react"
import { useEffect, useRef, useState } from "react"
import * as THREE from "three"
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls"
import { GLTFLoader } from "three/examples/jsm/loaders/GLTFLoader"
import TWEEN from "@tweenjs/tween.js"

interface SceneViewProps {
  code: string
}

const SceneView: React.FC<SceneViewProps> = ({ code }) => {
  const containerRef = useRef<HTMLDivElement>(null)
  const [error, setError] = useState<string | null>(null)
  const [debugInfo, setDebugInfo] = useState<string>("")

  // Persistent refs for scene, camera, renderer, and controls
  const sceneRef = useRef<THREE.Scene | null>(null)
  const cameraRef = useRef<THREE.PerspectiveCamera | null>(null)
  const rendererRef = useRef<THREE.WebGLRenderer | null>(null)
  const controlsRef = useRef<OrbitControls | null>(null)

  useEffect(() => {
    if (!containerRef.current) return

    // Create the scene, camera, renderer, and controls once
    if (!sceneRef.current) {
      sceneRef.current = new THREE.Scene()
      cameraRef.current = new THREE.PerspectiveCamera(
        75,
        containerRef.current.clientWidth / containerRef.current.clientHeight,
        0.1,
        1000,
      )
      rendererRef.current = new THREE.WebGLRenderer({ antialias: true })
      rendererRef.current.setSize(containerRef.current.clientWidth, containerRef.current.clientHeight)
      containerRef.current.appendChild(rendererRef.current.domElement)

      // Set initial camera position for better 3D viewing
      cameraRef.current.position.set(0, 0, 5)
      cameraRef.current.lookAt(0, 0, 0)

      // Add OrbitControls
      controlsRef.current = new OrbitControls(cameraRef.current, rendererRef.current.domElement)
      controlsRef.current.enableDamping = true
      controlsRef.current.dampingFactor = 0.25
    }

    const scene = sceneRef.current!
    const camera = cameraRef.current!
    const renderer = rendererRef.current!
    const controls = controlsRef.current!

    // Clear existing objects from the scene
    while (scene.children.length > 0) {
      scene.remove(scene.children[0])
    }

    // Add ambient light for better 3D visibility
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.5)
    scene.add(ambientLight)

    // Add directional light for 3D shading
    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.5)
    directionalLight.position.set(5, 5, 5)
    scene.add(directionalLight)

    const addDebugInfo = (info: string) => {
      setDebugInfo((prev) => prev + info + "\n")
    }

    // Define a generic helper function for create-or-update logic
    function updateOrCreate(name: string, createFn: () => THREE.Object3D, updateFn: (object: THREE.Object3D) => void) {
      let object = scene.getObjectByName(name)
      if (object) {
        updateFn(object)
      } else {
        object = createFn()
        if (!object) {
          console.error("Error: createFn did not return a valid object for", name)
          return
        }
        object.name = name
        scene.add(object)
      }
      return object
    }

    // Helper function to load 3D models
    const loadModel = (url: string) => {
      return new Promise((resolve, reject) => {
        const loader = new GLTFLoader()
        loader.load(
          url,
          (gltf) => {
            resolve(gltf.scene)
          },
          undefined,
          (error) => {
            reject(error)
          },
        )
      })
    }

    // Execute the injected code with context that includes the helpers
    try {
      const executeCode = new Function(
        "THREE",
        "scene",
        "camera",
        "renderer",
        "controls",
        "updateOrCreate",
        "addDebugInfo",
        "loadModel",
        "TWEEN",
        `
        try {
          ${code}
          //addDebugInfo("Custom code executed successfully");
        } catch (error) {
          addDebugInfo("Error executing custom code: " + error.message);
          throw error;
        }
      `,
      )

      executeCode(THREE, scene, camera, renderer, controls, updateOrCreate, addDebugInfo, loadModel, TWEEN)
    } catch (error) {
      console.error("Error executing WebGL code:", error)
      setError(error.message)
    }

    // Animation loop
    const animate = () => {
      requestAnimationFrame(animate)
      TWEEN.update()
      controls.update()
      renderer.render(scene, camera)
    }
    animate()

    // Handle window resize
    const handleResize = () => {
      if (containerRef.current) {
        const width = containerRef.current.clientWidth
        const height = containerRef.current.clientHeight
        camera.aspect = width / height
        camera.updateProjectionMatrix()
        renderer.setSize(width, height)
      }
    }

    window.addEventListener("resize", handleResize)

    return () => {
      window.removeEventListener("resize", handleResize)
    }
  }, [code])

  if (error) {
    return (
      <div className="text-red-500 p-4">
        <p>Error: {error}</p>
        <pre className="mt-4 p-2 bg-gray-800 text-white rounded">{debugInfo}</pre>
      </div>
    )
  }

  return (
    <div className="relative w-full h-full">
      <div ref={containerRef} className="w-full h-full bg-black rounded-xl" />
      {debugInfo && (
        <pre className="absolute top-0 left-0 p-2 bg-black bg-opacity-50 text-white text-xs">{debugInfo}</pre>
      )}
    </div>
  )
}

export default SceneView

