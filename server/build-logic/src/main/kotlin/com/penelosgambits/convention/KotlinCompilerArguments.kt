package com.penelosgambits.convention

import org.gradle.api.Project
import org.jetbrains.kotlin.gradle.dsl.KotlinJvmProjectExtension

fun Project.addCompilerArguments() {
    extensions.configure(KotlinJvmProjectExtension::class.java) {
        compilerOptions.freeCompilerArgs.add("-Xcontext-parameters")
    }
}
