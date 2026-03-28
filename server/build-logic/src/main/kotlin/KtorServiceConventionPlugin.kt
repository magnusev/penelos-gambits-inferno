import com.penelosgambits.convention.addCompilerArguments
import com.penelosgambits.convention.addDependency
import com.penelosgambits.convention.addDependencyBundle
import com.penelosgambits.convention.addProjectDependency
import com.penelosgambits.convention.applyDetektConfiguration
import com.penelosgambits.convention.applyKtlintConfiguration
import com.penelosgambits.convention.applyTestConfiguration
import com.penelosgambits.convention.configureJibDocker
import com.penelosgambits.convention.extentions.KtorServiceExtension
import com.penelosgambits.convention.extentions.collectSettings
import org.gradle.api.Plugin
import org.gradle.api.Project
import org.gradle.api.plugins.JavaApplication
import org.gradle.kotlin.dsl.configure

class KtorServiceConventionPlugin : Plugin<Project> {

    override fun apply(target: Project) = with(target) {
        applyPlugins()
        applyKtlintConfiguration()
        applyDetektConfiguration()

        addCommonDependencies()
        applyTestConfiguration()

        addCompilerArguments()

        val ext = extensions.create("ktorService", KtorServiceExtension::class.java)

        afterEvaluate {
            val settings = collectSettings(ext)
            configureJibDocker(settings)

            configure<JavaApplication> {
                settings.mainClass.let { mainClass.set(it) }
            }
        }

    }

    private fun Project.applyPlugins() {
        pluginManager.apply("org.jetbrains.kotlin.jvm")
        pluginManager.apply("org.jetbrains.kotlin.plugin.serialization")
        pluginManager.apply("application")
        pluginManager.apply("com.google.cloud.tools.jib")
        pluginManager.apply("io.ktor.plugin")
    }

    private fun Project.addCommonDependencies() {
        addDependencyBundle("ktor-server")
        addDependency("logback-classic")
        addDependency("logstash-logback-encoder")

        addDependency("ktor-server-testHost", configurationName = "testImplementation")

        addProjectDependency(":backend:libs:ulid")
        addProjectDependency(":backend:libs:ktor-common")
    }

}
