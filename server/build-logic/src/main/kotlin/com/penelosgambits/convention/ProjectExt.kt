package com.penelosgambits.convention

import org.gradle.api.Project
import org.gradle.api.artifacts.VersionCatalogsExtension

fun Project.addDependency(
    libraryName: String,
    configurationName: String = "implementation",
    catalog: String = "libs"
) {
    val libs = extensions.getByType(VersionCatalogsExtension::class.java)
        .named(catalog)

    val library = libs.findLibrary(libraryName).get()

    dependencies.add(configurationName, library)
}

fun Project.addDependencyBundle(
    bundleName: String,
    configurationName: String = "implementation",
    catalog: String = "libs"
) {

    val libs = extensions.getByType(VersionCatalogsExtension::class.java)
        .named(catalog)

    val bundle = libs.findBundle(bundleName).get()

    dependencies.add(configurationName, bundle)
}

fun Project.addProjectDependency(
    path: String,
    configurationName: String = "implementation"
) {
    dependencies.add(configurationName, project(path))
}
