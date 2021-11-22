call tracer\build.cmd CompileManagedSrc

pushd tracer\test\test-applications\security\Samples.Haproxy
docker-compose up --build 
popd 